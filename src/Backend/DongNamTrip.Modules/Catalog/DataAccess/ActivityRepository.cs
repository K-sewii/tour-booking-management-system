using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Hoạt động và suất của địa điểm cho quản trị (SCR15).</summary>
public sealed record ActivityContext(int Id, int DestinationId, string Status, string DestinationStatus, int DurationMinutes, TimeOnly? OpenTime, TimeOnly? CloseTime);

public sealed record SlotState(int Id, int ActivityId, DateOnly Date, TimeOnly Start, TimeOnly End, string Status);

public sealed record TimeRange(TimeOnly Start, TimeOnly End);

public sealed record SlotSave(SlotInfo? Slot, bool Duplicate);

public sealed class ActivityRepository(DongNamTripDbContext db)
{
    private static SlotInfo ToSlot(Suat s) => new(s.MaSuat, s.MaHd, s.Ngay, s.GioBatDau, s.GioKetThuc, s.TrangThai);

    private static AdminActivity ToDto(Hoatdong a) => new(a.MaHd, a.MaDd, a.MaDdNavigation.TenDd, a.TenHd, a.MoTa, a.ThoiLuongPhut, a.DieuKien, a.PhiThamKhao, a.TrangThai,
        a.Suat.OrderBy(s => s.Ngay).ThenBy(s => s.GioBatDau).Select(ToSlot).ToList());

    public async Task<List<AdminActivity>> ListAsync(int? destinationId, CancellationToken ct)
    {
        var query = db.Hoatdong.AsNoTracking().Include(x => x.Suat).Include(x => x.MaDdNavigation).AsQueryable();
        if (destinationId is { } id) query = query.Where(x => x.MaDd == id);
        return (await query.OrderBy(x => x.MaDd).ThenBy(x => x.MaHd).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public async Task<AdminActivity?> GetAsync(int id, CancellationToken ct)
    {
        var row = await db.Hoatdong.AsNoTracking().Include(x => x.Suat).Include(x => x.MaDdNavigation).FirstOrDefaultAsync(x => x.MaHd == id, ct);
        return row is null ? null : ToDto(row);
    }

    public Task<ActivityContext?> ContextAsync(int id, CancellationToken ct) =>
        db.Hoatdong.AsNoTracking().Where(x => x.MaHd == id)
            .Select(x => new ActivityContext(x.MaHd, x.MaDd, x.TrangThai, x.MaDdNavigation.TrangThai, x.ThoiLuongPhut, x.MaDdNavigation.GioMoCua, x.MaDdNavigation.GioDongCua))
            .FirstOrDefaultAsync(ct);

    public Task<bool> PlaceExistsAsync(int destinationId, CancellationToken ct) => db.Diadiem.AnyAsync(x => x.MaDd == destinationId, ct);

    public Task<bool> DuplicateNameAsync(int excludeId, int destinationId, string name, CancellationToken ct) =>
        db.Hoatdong.AnyAsync(x => x.MaHd != excludeId && x.MaDd == destinationId && x.TenHd == name, ct);

    /// <summary>Hoạt động có suất đang nằm trong lịch trình của khách (không được chuyển sang địa điểm khác).</summary>
    public Task<bool> InSchedulesAsync(int activityId, CancellationToken ct) =>
        db.LichtrinhSuat.AnyAsync(x => x.MaSuatNavigation.MaHd == activityId, ct);

    public Task<int> AffectedByActivityAsync(int activityId, CancellationToken ct) =>
        db.LichtrinhSuat.Where(x => x.MaSuatNavigation.MaHd == activityId).Select(x => x.MaLt).Distinct().CountAsync(ct);

    public Task<int> AffectedBySlotAsync(int slotId, CancellationToken ct) =>
        db.LichtrinhSuat.Where(x => x.MaSuat == slotId).Select(x => x.MaLt).Distinct().CountAsync(ct);

    /// <summary>Tạo mới (<paramref name="id"/> null, trạng thái Draft) hoặc cập nhật; null nếu không tìm thấy.</summary>
    public async Task<AdminActivity?> SaveAsync(int? id, ActivityForm form, CancellationToken ct)
    {
        Hoatdong? row;
        if (id is null)
        {
            row = new Hoatdong { TrangThai = ContentStatus.Draft };
            db.Hoatdong.Add(row);
        }
        else if ((row = await db.Hoatdong.SingleOrDefaultAsync(x => x.MaHd == id, ct)) is null)
            return null;
        row.MaDd = form.DestinationId;
        row.TenHd = form.Name;
        row.MoTa = form.Description;
        row.ThoiLuongPhut = form.DurationMinutes;
        row.DieuKien = form.Conditions;
        row.PhiThamKhao = form.ReferenceFee;
        await db.SaveChangesAsync(ct);
        return await GetAsync(row.MaHd, ct);
    }

    public async Task<bool> SetStatusAsync(int id, string status, CancellationToken ct) =>
        await db.Hoatdong.Where(x => x.MaHd == id).ExecuteUpdateAsync(set => set.SetProperty(x => x.TrangThai, status), ct) > 0;

    // ---- Suất ----

    public Task<SlotState?> SlotAsync(int id, CancellationToken ct) =>
        db.Suat.AsNoTracking().Where(x => x.MaSuat == id)
            .Select(x => new SlotState(x.MaSuat, x.MaHd, x.Ngay, x.GioBatDau, x.GioKetThuc, x.TrangThai))
            .FirstOrDefaultAsync(ct);

    /// <summary>Các suất khác của hoạt động trong cùng ngày, để kiểm tra trùng giờ.</summary>
    public async Task<List<TimeRange>> RangesAsync(int activityId, DateOnly date, int excludeSlotId, CancellationToken ct) =>
        await db.Suat.AsNoTracking().Where(x => x.MaHd == activityId && x.MaSuat != excludeSlotId && x.Ngay == date)
            .Select(x => new TimeRange(x.GioBatDau, x.GioKetThuc)).ToListAsync(ct);

    public async Task<SlotSave> SaveSlotAsync(int? slotId, int activityId, SlotForm form, CancellationToken ct)
    {
        Suat? row;
        if (slotId is null)
        {
            row = new Suat { MaHd = activityId, TrangThai = ContentStatus.Published };
            db.Suat.Add(row);
        }
        else if ((row = await db.Suat.SingleOrDefaultAsync(x => x.MaSuat == slotId, ct)) is null)
            return new SlotSave(null, false);
        row.Ngay = form.Date;
        row.GioBatDau = form.Start;
        row.GioKetThuc = form.End;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (DbErrors.IsDuplicateKey(exception))
        {
            db.Entry(row).State = EntityState.Detached;
            return new SlotSave(null, true);
        }
        return new SlotSave(ToSlot(row), false);
    }

    public async Task<bool> SetSlotStatusAsync(int id, string status, CancellationToken ct) =>
        await db.Suat.Where(x => x.MaSuat == id).ExecuteUpdateAsync(set => set.SetProperty(x => x.TrangThai, status), ct) > 0;
}
