using System.Data;
using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Itinerary;

public sealed record ItineraryData(int Id, int UserId, string Name, DateOnly StartDate, int Guests,
    string? DeparturePoint, string? Transport, decimal? Budget, DateTime UpdatedAt, ItineraryStopDraft[] Stops);

/// <summary>Reads and writes LICHTRINH and its stops/desired slots.</summary>
public sealed class ItineraryRepository(DongNamTripDbContext db)
{
    public async Task<List<ItineraryData>> ListAsync(int accountId, CancellationToken ct)
    {
        var rows = await db.Lichtrinh.AsNoTracking().Where(item => item.MaTk == accountId)
            .OrderByDescending(item => item.NgayCapNhat).ToListAsync(ct);
        var result = new List<ItineraryData>(rows.Count);
        foreach (var row in rows) result.Add(await ToDataAsync(row, ct));
        return result;
    }

    public async Task<ItineraryData?> GetAsync(int id, int accountId, CancellationToken ct)
    {
        var row = await db.Lichtrinh.AsNoTracking().SingleOrDefaultAsync(item => item.MaLt == id && item.MaTk == accountId, ct);
        return row is null ? null : await ToDataAsync(row, ct);
    }

    public async Task<int?> SaveAsync(int? id, int accountId, ItineraryDraft draft, ItineraryStopDraft[] stops, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var row = id is null ? new Lichtrinh { MaTk = accountId } :
            await db.Lichtrinh.SingleOrDefaultAsync(item => item.MaLt == id && item.MaTk == accountId, ct);
        if (row is null) return null;
        row.TenLt = draft.Name.Trim();
        row.NgayDi = draft.StartDate;
        row.SoKhach = draft.Guests;
        row.NoiXuatPhat = draft.DeparturePoint?.Trim();
        row.PhuongTien = draft.Transport?.Trim();
        row.NganSach = draft.Budget;
        row.NgayCapNhat = DateTime.UtcNow;
        if (id is null) { db.Lichtrinh.Add(row); await db.SaveChangesAsync(ct); }
        else
        {
            await db.LichtrinhSuat.Where(item => item.MaLt == row.MaLt).ExecuteDeleteAsync(ct);
            await db.LichtrinhDiadiem.Where(item => item.MaLt == row.MaLt).ExecuteDeleteAsync(ct);
        }
        foreach (var stop in stops)
            db.LichtrinhDiadiem.Add(new LichtrinhDiadiem { MaLt = row.MaLt, MaDd = stop.DestinationId,
                NgayThu = stop.Day, ThuTu = stop.Order, GhiChu = stop.Note?.Trim() });
        await db.SaveChangesAsync(ct);
        foreach (var stop in stops)
            foreach (var slotId in stop.SlotIds)
                db.LichtrinhSuat.Add(new LichtrinhSuat { MaLt = row.MaLt, MaDd = stop.DestinationId, MaSuat = slotId });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return row.MaLt;
    }

    private async Task<ItineraryData> ToDataAsync(Lichtrinh row, CancellationToken ct)
    {
        var stops = await db.LichtrinhDiadiem.AsNoTracking().Where(item => item.MaLt == row.MaLt)
            .OrderBy(item => item.NgayThu).ThenBy(item => item.ThuTu).ToListAsync(ct);
        var slots = await db.LichtrinhSuat.AsNoTracking().Where(item => item.MaLt == row.MaLt).ToListAsync(ct);
        return new(row.MaLt, row.MaTk, row.TenLt, row.NgayDi, row.SoKhach, row.NoiXuatPhat, row.PhuongTien, row.NganSach, row.NgayCapNhat,
            stops.Select(stop => new ItineraryStopDraft { DestinationId = stop.MaDd, Day = stop.NgayThu, Order = stop.ThuTu,
                Note = stop.GhiChu, SlotIds = slots.Where(slot => slot.MaDd == stop.MaDd).OrderBy(slot => slot.MaSuat).Select(slot => slot.MaSuat).ToArray() }).ToArray());
    }
}
