using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Administration;

/// <summary>Chỉ đọc DONDAT, GIAODICHTIEN và YEUCAUTOURRIENG qua ngữ cảnh chung để thống kê (UC24, SCR13).</summary>
public sealed class StatisticsRepository(DongNamTripDbContext db)
{
    public async Task<List<StatusCount>> OrdersByStatusAsync(DateTime from, DateTime to, string? status, CancellationToken ct)
    {
        var query = db.Dondat.AsNoTracking().Where(item => item.NgayDat >= from && item.NgayDat < to);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.TrangThai == status);
        return (await query.GroupBy(item => item.TrangThai).Select(group => new { group.Key, Count = group.Count(), Value = group.Sum(item => item.GiaChot) }).ToListAsync(ct))
            .Select(row => new StatusCount(row.Key, row.Count, row.Key is OrderStatus.Cancelled or OrderStatus.Expired ? 0 : row.Value)).OrderBy(row => row.Status).ToList();
    }

    public async Task<(decimal Received, decimal Refunded)> MoneyTotalsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var rows = await db.Giaodichtien.AsNoTracking().Where(item => item.ThoiDiem >= from && item.ThoiDiem < to)
            .GroupBy(item => item.LoaiGd).Select(group => new { group.Key, Sum = group.Sum(item => item.SoTien) }).ToListAsync(ct);
        return (rows.Where(row => row.Key == MoneyKinds.Receipt).Sum(row => row.Sum), rows.Where(row => row.Key == MoneyKinds.Refund).Sum(row => row.Sum));
    }

    public async Task<List<StatusCount>> RequestsByStatusAsync(DateTime from, DateTime to, CancellationToken ct) =>
        (await db.Yeucautourrieng.AsNoTracking().Where(item => item.NgayGui >= from && item.NgayGui < to)
            .GroupBy(item => item.TrangThai).Select(group => new { group.Key, Count = group.Count() }).ToListAsync(ct))
        .Select(row => new StatusCount(row.Key, row.Count)).OrderBy(row => row.Status).ToList();
}
