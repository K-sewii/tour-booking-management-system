using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DongNamTrip.Modules.Administration;

/// <summary>Truy cập dữ liệu quản lý người dùng và vai trò (UC24, SCR19).</summary>
public sealed class UserAdminRepository(DongNamTripDbContext db)
{
    public Task<IDbContextTransaction> BeginAsync(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);

    /// <summary>Tuần tự hóa các thay đổi vai trò/trạng thái đến hết giao dịch hiện tại, để kiểm tra "Admin cuối cùng" không bị hai yêu cầu đồng thời vượt qua.
    /// Chờ tối đa 5 giây; false khi không lấy được khóa (mã trả về của sp_getapplock nhỏ hơn 0). Phải gọi trong giao dịch.</summary>
    public async Task<bool> LockAdminChangesAsync(CancellationToken ct) =>
        (await db.Database.SqlQueryRaw<int>("DECLARE @r int; EXEC @r = sp_getapplock @Resource = 'DongNamTrip.AdminChanges', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000; SELECT @r AS Value;")
            .ToListAsync(ct)).Single() >= 0;

    public async Task<(List<AdminUser> Rows, int Total)> ListAsync(UserFilter filter, int skip, int take, CancellationToken ct)
    {
        var query = db.Taikhoan.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(item => item.HoTen.Contains(q) || item.Email.Contains(q));
        }
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(item => item.TrangThai == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Role)) query = query.Where(item => item.TaikhoanVaitro.Any(link => link.MaVaiTroNavigation.TenVaiTro == filter.Role));
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(item => item.MaTk).Skip(skip).Take(take)
            .Select(item => new { item.MaTk, item.Email, item.HoTen, item.SoDienThoai, item.TrangThai, item.NgayTao, Roles = item.TaikhoanVaitro.Select(link => link.MaVaiTroNavigation.TenVaiTro).ToList() })
            .ToListAsync(ct);
        return (rows.Select(row => new AdminUser(row.MaTk, row.Email, row.HoTen, row.SoDienThoai, row.TrangThai, row.Roles.Order().ToList(), row.NgayTao)).ToList(), total);
    }

    public async Task<AdminUser?> GetAsync(int id, CancellationToken ct)
    {
        var row = await db.Taikhoan.AsNoTracking().Where(item => item.MaTk == id)
            .Select(item => new { item.MaTk, item.Email, item.HoTen, item.SoDienThoai, item.TrangThai, item.NgayTao, Roles = item.TaikhoanVaitro.Select(link => link.MaVaiTroNavigation.TenVaiTro).ToList() })
            .FirstOrDefaultAsync(ct);
        return row is null ? null : new AdminUser(row.MaTk, row.Email, row.HoTen, row.SoDienThoai, row.TrangThai, row.Roles.Order().ToList(), row.NgayTao);
    }

    public Task<Taikhoan?> AccountAsync(int id, CancellationToken ct) => db.Taikhoan.AsNoTracking().FirstOrDefaultAsync(item => item.MaTk == id, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => db.Taikhoan.AnyAsync(item => item.Email == email, ct);

    /// <summary>Tên vai trò có trong bảng VAITRO (để kiểm tra dữ liệu cấu hình).</summary>
    public async Task<Dictionary<string, int>> RoleIdsAsync(CancellationToken ct) =>
        await db.Vaitro.AsNoTracking().ToDictionaryAsync(item => item.TenVaiTro, item => item.MaVaiTro, ct);

    /// <summary>Vai trò được yêu cầu mà bảng VAITRO chưa có (dữ liệu cấu hình thiếu).</summary>
    public async Task<List<string>> MissingRolesAsync(IEnumerable<string> roles, CancellationToken ct)
    {
        var ids = await RoleIdsAsync(ct);
        return roles.Where(role => !ids.ContainsKey(role)).ToList();
    }

    /// <summary>Thêm tài khoản và vai trò; trả null khi email trùng (hai yêu cầu đồng thời cùng qua kiểm tra trước).</summary>
    public async Task<int?> AddAsync(Taikhoan account, IReadOnlyCollection<string> roles, CancellationToken ct)
    {
        var ids = await RoleIdsAsync(ct);
        try
        {
            db.Taikhoan.Add(account);
            await db.SaveChangesAsync(ct);
            db.TaikhoanVaitro.AddRange(roles.Select(role => new TaikhoanVaitro { MaTk = account.MaTk, MaVaiTro = ids[role] }));
            await db.SaveChangesAsync(ct);
            return account.MaTk;
        }
        catch (DbUpdateException exception) when (DbErrors.IsDuplicateKey(exception)) { db.ChangeTracker.Clear(); return null; }
    }

    public async Task ReplaceRolesAsync(int id, IReadOnlyCollection<string> roles, CancellationToken ct)
    {
        var ids = await RoleIdsAsync(ct);
        await db.TaikhoanVaitro.Where(item => item.MaTk == id).ExecuteDeleteAsync(ct);
        db.TaikhoanVaitro.AddRange(roles.Select(role => new TaikhoanVaitro { MaTk = id, MaVaiTro = ids[role] }));
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> SetStatusAsync(int id, string status, CancellationToken ct) =>
        await db.Taikhoan.Where(item => item.MaTk == id).ExecuteUpdateAsync(set => set.SetProperty(item => item.TrangThai, status), ct) == 1;

    public async Task<bool> SetPasswordHashAsync(int id, string hash, CancellationToken ct) =>
        await db.Taikhoan.Where(item => item.MaTk == id).ExecuteUpdateAsync(set => set.SetProperty(item => item.MatKhauBam, hash), ct) == 1;

    /// <summary>Số tài khoản Admin đang hoạt động.</summary>
    public Task<int> CountAdminsAsync(CancellationToken ct) =>
        db.Taikhoan.CountAsync(item => item.TrangThai == AccountStatus.Active && item.TaikhoanVaitro.Any(link => link.MaVaiTroNavigation.TenVaiTro == AppRoles.Admin), ct);
}
