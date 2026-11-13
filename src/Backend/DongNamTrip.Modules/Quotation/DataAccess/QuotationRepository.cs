using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DongNamTrip.Modules.Quotation;

/// <summary>
/// Ghi YEUCAUTOURRIENG, XACMINHDICHVU, BAOGIA, HANGMUCBAOGIA. Chỉ đọc PHIENBANLICHTRINH và LICHTRINH (để lọc theo chủ và lấy số phiên bản);
/// mọi ghi vào lịch trình đi qua IItineraryApi.
/// </summary>
public sealed class QuotationRepository(DongNamTripDbContext db)
{
    public Task<IDbContextTransaction> BeginAsync(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);

    private static readonly string[] Open = [QuoteRequestStatus.Submitted, QuoteRequestStatus.Verifying, QuoteRequestStatus.Quoted];

    private IQueryable<Yeucautourrieng> Requests => db.Yeucautourrieng.AsNoTracking().Include(item => item.MaPbNavigation);

    /// <summary>Yêu cầu đang mở (Submitted, Verifying, Quoted) của lịch trình thuộc tài khoản.</summary>
    public Task<Yeucautourrieng?> OpenRequestAsync(int accountId, int itineraryId, CancellationToken ct) =>
        Requests.Where(item => item.MaPbNavigation.MaLt == itineraryId && item.MaPbNavigation.MaLtNavigation.MaTk == accountId && Open.Contains(item.TrangThai))
            .OrderByDescending(item => item.MaYc).FirstOrDefaultAsync(ct);

    public async Task<int> AddRequestAsync(Yeucautourrieng row, CancellationToken ct)
    {
        db.Yeucautourrieng.Add(row);
        await db.SaveChangesAsync(ct);
        return row.MaYc;
    }

    public Task<Yeucautourrieng?> RequestAsync(int id, CancellationToken ct) => Requests.FirstOrDefaultAsync(item => item.MaYc == id, ct);

    public async Task<(List<Yeucautourrieng> Rows, int Total)> ListAsync(QuoteRequestFilter filter, int skip, int take, CancellationToken ct)
    {
        var query = Requests;
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(item => item.TrangThai == filter.Status);
        if (filter.From is { } from) query = query.Where(item => item.NgayGui >= from);
        if (filter.To is { } to) query = query.Where(item => item.NgayGui <= to);
        var total = await query.CountAsync(ct);
        return (await query.OrderByDescending(item => item.MaYc).Skip(skip).Take(take).ToListAsync(ct), total);
    }

    public Task<List<Yeucautourrieng>> MineAsync(int accountId, CancellationToken ct) =>
        Requests.Where(item => item.MaPbNavigation.MaLtNavigation.MaTk == accountId).OrderByDescending(item => item.MaYc).ToListAsync(ct);

    public async Task<int?> OwnerAsync(int requestId, CancellationToken ct) =>
        await db.Yeucautourrieng.AsNoTracking().Where(item => item.MaYc == requestId).Select(item => (int?)item.MaPbNavigation.MaLtNavigation.MaTk).FirstOrDefaultAsync(ct);

    public async Task<bool> MoveRequestAsync(int id, string from, string to, CancellationToken ct) =>
        await db.Yeucautourrieng.Where(item => item.MaYc == id && item.TrangThai == from)
            .ExecuteUpdateAsync(set => set.SetProperty(item => item.TrangThai, to), ct) == 1;

    public async Task<int> AddVerificationAsync(Xacminhdichvu row, CancellationToken ct)
    {
        db.Xacminhdichvu.Add(row);
        await db.SaveChangesAsync(ct);
        return row.MaXm;
    }

    public Task<List<Xacminhdichvu>> VerificationsAsync(int requestId, CancellationToken ct) =>
        db.Xacminhdichvu.AsNoTracking().Where(item => item.MaYc == requestId).OrderBy(item => item.MaXm).ToListAsync(ct);

    /// <summary>Thêm báo giá cùng hạng mục; null khi vi phạm UX_BAOGIA_MaYC_Sent (hai nhân viên phát hành đồng thời).</summary>
    public async Task<int?> AddQuoteAsync(Baogia quote, IReadOnlyList<Hangmucbaogia> items, CancellationToken ct)
    {
        foreach (var item in items) quote.Hangmucbaogia.Add(item);
        db.Baogia.Add(quote);
        try { await db.SaveChangesAsync(ct); return quote.MaBg; }
        catch (DbUpdateException exception) when (DbErrors.IsDuplicateKey(exception)) { db.ChangeTracker.Clear(); return null; }
    }

    /// <summary>Mã báo giá hiện tại (mới nhất chưa bị thay thế) của từng yêu cầu, một truy vấn cho cả danh sách.</summary>
    public async Task<Dictionary<int, int>> CurrentQuoteIdsAsync(IReadOnlyCollection<int> requestIds, CancellationToken ct) =>
        (await db.Baogia.AsNoTracking().Where(item => requestIds.Contains(item.MaYc) && item.TrangThai != QuoteStatus.Superseded)
            .GroupBy(item => item.MaYc).Select(group => new { Request = group.Key, Quote = group.Max(item => item.MaBg) }).ToListAsync(ct))
        .ToDictionary(item => item.Request, item => item.Quote);

    public Task<List<Baogia>> QuotesAsync(int requestId, CancellationToken ct) =>
        db.Baogia.AsNoTracking().Include(item => item.Hangmucbaogia).Where(item => item.MaYc == requestId).OrderBy(item => item.MaBg).ToListAsync(ct);

    public Task<Baogia?> QuoteAsync(int id, CancellationToken ct) =>
        db.Baogia.AsNoTracking().Include(item => item.Hangmucbaogia).FirstOrDefaultAsync(item => item.MaBg == id, ct);

    /// <summary>Báo giá Sent của yêu cầu chuyển sang Superseded.</summary>
    public Task<int> SupersedeAsync(int requestId, CancellationToken ct) =>
        db.Baogia.Where(item => item.MaYc == requestId && item.TrangThai == QuoteStatus.Sent)
            .ExecuteUpdateAsync(set => set.SetProperty(item => item.TrangThai, QuoteStatus.Superseded), ct);

    public async Task<bool> MoveQuoteAsync(int id, string from, string to, CancellationToken ct) =>
        await db.Baogia.Where(item => item.MaBg == id && item.TrangThai == from)
            .ExecuteUpdateAsync(set => set.SetProperty(item => item.TrangThai, to), ct) == 1;

    public Task<List<Baogia>> ExpiredQuotesAsync(DateTime now, CancellationToken ct) =>
        db.Baogia.AsNoTracking().Where(item => item.TrangThai == QuoteStatus.Sent && item.HanHieuLuc < now).ToListAsync(ct);
}
