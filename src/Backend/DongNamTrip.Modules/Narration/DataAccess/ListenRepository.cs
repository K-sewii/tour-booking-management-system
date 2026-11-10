using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Narration;

public sealed record RightSave(int Id, ListenRightGrant? Before);

/// <summary>Quyền nghe và danh sách thuyết minh được phép nghe (SCR11, UC23).</summary>
public sealed class ListenRepository(DongNamTripDbContext db)
{
    private IQueryable<Quyennghe> Active(int accountId, DateTime now) =>
        db.Quyennghe.AsNoTracking().Where(x => x.MaTk == accountId && x.HieuLucTu <= now && x.HieuLucDen >= now);

    private static ListenRightGrant ToDto(Quyennghe x) => new(x.MaDon, x.MaTk, x.HieuLucTu, x.HieuLucDen);

    /// <summary>Hạn nghe của các quyền đang còn hiệu lực.</summary>
    public Task<List<DateTime>> ActiveUntilAsync(int accountId, DateTime now, CancellationToken ct) =>
        Active(accountId, now).Select(x => x.HieuLucDen).ToListAsync(ct);

    public Task<bool> EverHadRightAsync(int accountId, CancellationToken ct) => db.Quyennghe.AsNoTracking().AnyAsync(x => x.MaTk == accountId, ct);

    /// <summary>Địa điểm thuộc chuyến đi (tour đã đặt hoặc lịch trình chốt của báo giá) của các đơn còn quyền nghe.</summary>
    public async Task<List<int>> AllowedDestinationsAsync(int accountId, DateTime now, CancellationToken ct)
    {
        var fromTours = await Active(accountId, now)
            .Where(x => x.MaDonNavigation.MaLkh != null)
            .SelectMany(x => x.MaDonNavigation.MaLkhNavigation!.MaTourNavigation.TourDiadiem.Select(t => t.MaDd))
            .ToListAsync(ct);
        var fromCustom = await Active(accountId, now)
            .Where(x => x.MaDonNavigation.MaBg != null)
            .SelectMany(x => x.MaDonNavigation.MaBgNavigation!.MaPbNavigation.MaLtNavigation.LichtrinhDiadiem.Select(t => t.MaDd))
            .ToListAsync(ct);
        return fromTours.Concat(fromCustom).Distinct().ToList();
    }

    // Lọc trên thực thể trước, chỉ chiếu sang ListenTrack ở bước cuối (EF Core không dịch được Where đặt sau Select dựng bằng constructor).
    private IQueryable<Tepaudio> Published => db.Tepaudio.AsNoTracking()
        .Where(a => a.TrangThai == ContentStatus.Published
            && a.MaBdNavigation.TrangThai == ContentStatus.Published
            && a.MaBdNavigation.MaTmNavigation.TrangThai == ContentStatus.Published
            && a.MaBdNavigation.MaTmNavigation.MaDdNavigation.TrangThai == ContentStatus.Published);

    private static IQueryable<ListenTrack> ToTracks(IQueryable<Tepaudio> source) => source
        .Select(a => new ListenTrack(a.MaTep, a.MaBdNavigation.MaTmNavigation.MaDd, a.MaBdNavigation.MaTmNavigation.MaDdNavigation.TenDd,
            a.MaBdNavigation.MaNn, a.MaBdNavigation.MaNnNavigation.TenNn, a.MaBdNavigation.MaTmNavigation.TieuDe, a.GiongDoc, a.MaBdNavigation.NoiDungDich));

    public Task<List<ListenTrack>> TracksAsync(IReadOnlyCollection<int> destinationIds, CancellationToken ct) =>
        ToTracks(Published.Where(a => destinationIds.Contains(a.MaBdNavigation.MaTmNavigation.MaDd))).ToListAsync(ct);

    public Task<ListenTrack?> TrackAsync(int audioId, CancellationToken ct) =>
        ToTracks(Published.Where(a => a.MaTep == audioId)).FirstOrDefaultAsync(ct);

    public Task<string?> FileNameAsync(int audioId, CancellationToken ct) =>
        db.Tepaudio.AsNoTracking().Where(x => x.MaTep == audioId).Select(x => (string?)x.DuongDan).FirstOrDefaultAsync(ct);

    // ---- Cấp / thu hồi (Đức gọi khi xác nhận / hủy đơn) ----

    /// <summary>Chủ tài khoản của đơn; null nếu không có đơn.</summary>
    public Task<int?> OrderOwnerAsync(int orderId, CancellationToken ct) =>
        db.Dondat.AsNoTracking().Where(x => x.MaDon == orderId).Select(x => (int?)x.MaTk).FirstOrDefaultAsync(ct);

    public async Task<RightSave> SaveRightAsync(ListenRightGrant grant, CancellationToken ct)
    {
        var row = await db.Quyennghe.FirstOrDefaultAsync(x => x.MaDon == grant.OrderId && x.MaTk == grant.AccountId, ct);
        ListenRightGrant? before = null;
        if (row is null)
        {
            row = new Quyennghe { MaDon = grant.OrderId, MaTk = grant.AccountId };
            db.Quyennghe.Add(row);
        }
        else before = ToDto(row);
        row.HieuLucTu = grant.From.ToUniversalTime();
        row.HieuLucDen = grant.To.ToUniversalTime();
        await db.SaveChangesAsync(ct);
        return new RightSave(row.MaQn, before);
    }

    /// <summary>Xóa mọi quyền nghe của đơn; trả về các quyền đã xóa để ghi nhật ký.</summary>
    public async Task<List<ListenRightGrant>> RemoveRightsAsync(int orderId, CancellationToken ct)
    {
        var rows = await db.Quyennghe.Where(x => x.MaDon == orderId).ToListAsync(ct);
        if (rows.Count == 0) return [];
        var removed = rows.Select(ToDto).ToList();
        db.Quyennghe.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
        return removed;
    }
}
