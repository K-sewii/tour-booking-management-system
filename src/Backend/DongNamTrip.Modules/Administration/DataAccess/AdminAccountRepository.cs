using System.Data;
using System.Text.Json;
using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Administration;

public sealed class AdminAccountRepository(DongNamTripDbContext db)
{
    public Task<List<string>> RolesAsync(CancellationToken ct) => db.Vaitro.AsNoTracking().OrderBy(x => x.TenVaiTro).Select(x => x.TenVaiTro).ToListAsync(ct);

    public async Task<List<AdminAccount>> AccountsAsync(string? query, string? role, CancellationToken ct)
    {
        var source = db.Taikhoan.AsNoTracking().Include(x => x.TaikhoanVaitro).ThenInclude(x => x.MaVaiTroNavigation).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query)) { var q = query.Trim(); source = source.Where(x => x.HoTen.Contains(q) || x.Email.Contains(q)); }
        if (!string.IsNullOrWhiteSpace(role)) source = source.Where(x => x.TaikhoanVaitro.Any(r => r.MaVaiTroNavigation.TenVaiTro == role));
        var accounts = await source.OrderBy(x => x.HoTen).ThenBy(x => x.MaTk).ToListAsync(ct);
        return accounts.Select(x => new AdminAccount(x.MaTk, x.HoTen, x.Email, x.TrangThai, x.TaikhoanVaitro.Select(r => r.MaVaiTroNavigation.TenVaiTro).Order().ToArray())).ToList();
    }

    public async Task<AdminAccountPage> AccountPageAsync(string group, string? query, string? role, string? status, int page, CancellationToken ct)
    {
        var staff = new[] { AppRoles.Admin, AppRoles.Operator, AppRoles.Editor, AppRoles.Finance, AppRoles.Publisher };
        var source = db.Taikhoan.AsNoTracking().Where(x => group == "staff"
            ? x.TaikhoanVaitro.Any(r => staff.Contains(r.MaVaiTroNavigation.TenVaiTro))
            : x.TaikhoanVaitro.Any(r => r.MaVaiTroNavigation.TenVaiTro == AppRoles.Customer)
                && !x.TaikhoanVaitro.Any(r => r.MaVaiTroNavigation.TenVaiTro != AppRoles.Customer));
        if (!string.IsNullOrWhiteSpace(query)) { var q = query.Trim(); source = source.Where(x => x.HoTen.Contains(q) || x.Email.Contains(q)); }
        if (!string.IsNullOrWhiteSpace(role)) source = source.Where(x => x.TaikhoanVaitro.Any(r => r.MaVaiTroNavigation.TenVaiTro == role));
        var active = await source.CountAsync(x => x.TrangThai == "Active", ct);
        var locked = await source.CountAsync(x => x.TrangThai == "Locked", ct);
        var groupTotal = await source.CountAsync(ct);
        if (!string.IsNullOrWhiteSpace(status)) source = source.Where(x => x.TrangThai == status);
        var count = await source.CountAsync(ct);
        var pages = Math.Max(1, (count + 49) / 50);
        page = Math.Min(page, pages);
        var rows = await source.Include(x => x.TaikhoanVaitro).ThenInclude(x => x.MaVaiTroNavigation)
            .OrderBy(x => x.HoTen).ThenBy(x => x.MaTk).Skip((page - 1) * 50).Take(50).ToListAsync(ct);
        return new(rows.Select(x => new AdminAccount(x.MaTk, x.HoTen, x.Email, x.TrangThai,
            x.TaikhoanVaitro.Select(r => r.MaVaiTroNavigation.TenVaiTro).Order().ToArray(), x.SoDienThoai)).ToList(), groupTotal, active, locked, page, pages, count);
    }

    public async Task<AdminMutation> RolesAsync(int id, int actor, AdminRoleUpdate input, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var account = await db.Taikhoan.Include(x => x.TaikhoanVaitro).ThenInclude(x => x.MaVaiTroNavigation).SingleOrDefaultAsync(x => x.MaTk == id, ct);
        if (account is null) return AdminMutation.NotFound;
        var before = account.TaikhoanVaitro.Select(x => x.MaVaiTroNavigation.TenVaiTro).Order(StringComparer.Ordinal).ToArray();
        if (!before.SequenceEqual(input.ExpectedRoles.Order(StringComparer.Ordinal))) return AdminMutation.Conflict;
        var requested = input.Roles.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (requested.Length == 0 || (actor == id && before.Contains(AppRoles.Admin) && !requested.Contains(AppRoles.Admin))) return AdminMutation.Invalid;
        var roles = await db.Vaitro.Where(x => requested.Contains(x.TenVaiTro)).ToListAsync(ct);
        if (roles.Count != requested.Length) return AdminMutation.Invalid;
        db.TaikhoanVaitro.RemoveRange(account.TaikhoanVaitro.Where(x => !requested.Contains(x.MaVaiTroNavigation.TenVaiTro)));
        foreach (var role in roles.Where(x => !before.Contains(x.TenVaiTro))) db.TaikhoanVaitro.Add(new TaikhoanVaitro { MaTk = id, MaVaiTro = role.MaVaiTro });
        Audit(actor, id, "Đổi vai trò", before, requested);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return AdminMutation.Done;
    }

    public async Task<AdminMutation> StatusAsync(int id, int actor, AdminStatusUpdate input, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var account = await db.Taikhoan.SingleOrDefaultAsync(x => x.MaTk == id, ct);
        if (account is null) return AdminMutation.NotFound;
        if (account.TrangThai != input.ExpectedStatus) return AdminMutation.Conflict;
        if (input.Status is not ("Active" or "Locked") || (actor == id && input.Status == "Locked")) return AdminMutation.Invalid;
        var before = account.TrangThai;
        account.TrangThai = input.Status;
        Audit(actor, id, "Đổi trạng thái", new { Status = before }, new { Status = input.Status });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return AdminMutation.Done;
    }

    public async Task<AdminAuditPage> AuditAsync(int page, string? actorType, DateOnly? from, DateOnly? to, string? query, CancellationToken ct)
    {
        // These account mutations are written only by the Admin-protected account endpoints.
        // Other historic entries have no role snapshot; current account roles are not evidence.
        var source = db.Nhatkythaotac.AsNoTracking();
        if (actorType == "staff") source = source.Where(x => x.MaTk != null && x.LoaiDoiTuong == "TAIKHOAN" && (x.HanhDong == "Đổi vai trò" || x.HanhDong == "Đổi trạng thái"));
        else if (actorType == "system") source = source.Where(x => x.MaTk == null && x.HanhDong == "SeedCatalogPublicData" && x.LoaiDoiTuong == "Catalog" && x.MaDoiTuong == "PublicCatalog.20261028");
        else if (actorType == "customer") source = source.Where(x => false); // No customer business-audit source with actor snapshots yet.
        if (!string.IsNullOrWhiteSpace(query)) { var term = query.Trim(); source = source.Where(x => x.HanhDong.Contains(term) || (x.MaTkNavigation != null && x.MaTkNavigation.HoTen.Contains(term))); }
        if (from is not null) { var start = from.Value.ToDateTime(TimeOnly.MinValue).AddHours(-7); source = source.Where(x => x.ThoiDiem >= start); }
        if (to is not null) { var end = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7); source = source.Where(x => x.ThoiDiem < end); }
        var count = await source.CountAsync(ct);
        var rows = await source.OrderByDescending(x => x.ThoiDiem).ThenByDescending(x => x.MaNk).Skip((page - 1) * 50).Take(51)
            .Select(x => new AdminAuditRow(x.MaNk, x.ThoiDiem, x.MaTkNavigation == null ? (x.MaTk == null && x.HanhDong == "SeedCatalogPublicData" && x.LoaiDoiTuong == "Catalog" && x.MaDoiTuong == "PublicCatalog.20261028" ? "Hệ thống" : "Không xác định") : x.MaTkNavigation.HoTen, x.HanhDong, x.LoaiDoiTuong, x.MaDoiTuong, x.LyDo, x.LoaiDoiTuong == "TAIKHOAN" && x.HanhDong == "Đổi vai trò" ? x.DuLieuTruoc : null, x.LoaiDoiTuong == "TAIKHOAN" && x.HanhDong == "Đổi vai trò" ? x.DuLieuSau : null,
                x.MaTk != null && x.LoaiDoiTuong == "TAIKHOAN" && (x.HanhDong == "Đổi vai trò" || x.HanhDong == "Đổi trạng thái") ? "staff" : x.MaTk == null && x.HanhDong == "SeedCatalogPublicData" && x.LoaiDoiTuong == "Catalog" && x.MaDoiTuong == "PublicCatalog.20261028" ? "system" : "unknown",
                x.LoaiDoiTuong == "TEPAUDIO" && x.HanhDong == "Tạo audio lỗi" ? "Tạo audio thất bại" : null)).ToListAsync(ct);
        return new AdminAuditPage(rows.Take(50).ToList(), rows.Count > 50, Math.Max(1, (count + 49) / 50), count);
    }

    private void Audit(int actor, int id, string action, object before, object after) => db.Nhatkythaotac.Add(new Nhatkythaotac { MaTk = actor, ThoiDiem = DateTime.UtcNow, HanhDong = action, LoaiDoiTuong = "TAIKHOAN", MaDoiTuong = id.ToString(), DuLieuTruoc = JsonSerializer.Serialize(before), DuLieuSau = JsonSerializer.Serialize(after) });
}
