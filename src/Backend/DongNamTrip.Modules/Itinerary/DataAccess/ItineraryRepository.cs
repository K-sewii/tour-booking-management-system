using System.Text.Json;
using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Itinerary;

/// <summary>Chỉ đọc/ghi LICHTRINH*; thông tin điểm đến lấy qua ICatalogApi.</summary>
public sealed class ItineraryRepository(DongNamTripDbContext db)
{
    public async Task<List<(int Id, string Name, DateOnly StartDate, int Guests, int[] DestinationIds)>> ListAsync(int accountId, CancellationToken ct)
    {
        var rows = await db.Lichtrinh.AsNoTracking().Where(item => item.MaTk == accountId).OrderBy(item => item.MaLt)
            .Select(item => new { item.MaLt, item.TenLt, item.NgayDi, item.SoKhach, Places = item.LichtrinhDiadiem.OrderBy(p => p.NgayThu).ThenBy(p => p.ThuTu).Select(p => p.MaDd).ToList() })
            .ToListAsync(ct);
        return rows.Select(row => (row.MaLt, row.TenLt, row.NgayDi, row.SoKhach, row.Places.ToArray())).ToList();
    }

    public async Task<int> AddAsync(int accountId, ItineraryDraft draft, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var trip = new Lichtrinh { MaTk = accountId, TenLt = draft.Name.Trim(), NgayDi = draft.StartDate, SoKhach = draft.Guests, NgayCapNhat = now };
        for (var index = 0; index < draft.DestinationIds.Length; index++)
            trip.LichtrinhDiadiem.Add(new LichtrinhDiadiem { MaDd = draft.DestinationIds[index], NgayThu = 1, ThuTu = index + 1 });
        trip.Phienbanlichtrinh.Add(new Phienbanlichtrinh
        {
            SoPhienBan = 1,
            ThoiDiemTao = now,
            NoiDungJson = JsonSerializer.Serialize(new { name = trip.TenLt, startDate = draft.StartDate, guests = draft.Guests, destinationIds = draft.DestinationIds })
        });
        db.Lichtrinh.Add(trip);
        await db.SaveChangesAsync(ct);
        return trip.MaLt;
    }
}
