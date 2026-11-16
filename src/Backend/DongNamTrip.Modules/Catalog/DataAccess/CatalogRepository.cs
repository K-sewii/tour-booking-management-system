using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using TourDto = DongNamTrip.Contracts.Tour;
using TourRow = DongNamTrip.Modules.Persistence.Entities.Tour;

namespace DongNamTrip.Modules.Catalog;

public sealed class CatalogRepository(DongNamTripDbContext db)
{
    // Trạng thái bản ghi: Draft, Published, Inactive. Chỉ Published hiển thị công khai.
    private const string Published = ContentStatus.Published;

    private IQueryable<Diadiem> VisiblePlaces => db.Diadiem.AsNoTracking().Where(item => item.TrangThai == Published);
    private IQueryable<TourRow> VisibleTours => db.Tour.AsNoTracking().Where(item => item.TrangThai == Published);

    private static Destination ToDto(Diadiem row) =>
        new(row.MaDd, row.TenDd, row.KhuVuc, row.LoaiHinh, row.DiaChi, row.MoTa, row.ThoiLuongPhut, CatalogOptions.IllustrationFor(row.KhuVuc), row.TrangThai, row.AnhMinhHoa);

    public Task<bool> PlaceExistsAsync(int id, CancellationToken ct) => db.Diadiem.AnyAsync(item => item.MaDd == id, ct);

    public async Task<PlannerCatalog> PlannerCatalogAsync(CancellationToken ct)
    {
        var places = await DestinationsAsync(null, ct);
        var activities = await db.Hoatdong.AsNoTracking()
            .Where(item => item.TrangThai == Published && item.MaDdNavigation.TrangThai == Published)
            .OrderBy(item => item.MaHd)
            .Select(item => new PlannerActivity(item.MaHd, item.MaDd, item.TenHd, item.ThoiLuongPhut, item.DieuKien, item.PhiThamKhao))
            .ToListAsync(ct);
        var slots = await db.Suat.AsNoTracking()
            .Where(item => item.TrangThai == Published && item.Ngay >= DateOnly.FromDateTime(DateTime.Today)
                && item.MaHdNavigation.TrangThai == Published && item.MaHdNavigation.MaDdNavigation.TrangThai == Published)
            .OrderBy(item => item.Ngay).ThenBy(item => item.GioBatDau)
            .Select(item => new PlannerSlot(item.MaSuat, item.MaHd, item.MaHdNavigation.MaDd, item.Ngay, item.GioBatDau, item.GioKetThuc, item.TrangThai))
            .ToListAsync(ct);
        return new(places, activities, slots);
    }

    public async Task<ItineraryStopDraft[]?> TourTemplateAsync(int tourId, CancellationToken ct)
    {
        if (!await VisibleTours.AnyAsync(item => item.MaTour == tourId, ct)) return null;
        return await db.TourDiadiem.AsNoTracking().Where(item => item.MaTour == tourId)
            .OrderBy(item => item.NgayThu).ThenBy(item => item.ThuTu)
            .Select(item => new ItineraryStopDraft { DestinationId = item.MaDd, Day = item.NgayThu, Order = item.ThuTu })
            .ToArrayAsync(ct);
    }

    public Task<List<PlannerSlot>> SlotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        db.Suat.AsNoTracking().Where(item => ids.Contains(item.MaSuat))
            .Select(item => new PlannerSlot(item.MaSuat, item.MaHd, item.MaHdNavigation.MaDd, item.Ngay, item.GioBatDau, item.GioKetThuc, item.TrangThai))
            .ToListAsync(ct);

    public async Task<List<ProgramStop>> TourProgramAsync(int tourId, CancellationToken ct) =>
        (await db.TourDiadiem.AsNoTracking().Where(item => item.MaTour == tourId).OrderBy(item => item.NgayThu).ThenBy(item => item.ThuTu)
            .Select(item => new { item.NgayThu, item.MaDdNavigation.TenDd }).ToListAsync(ct))
        .Select(item => new ProgramStop(item.NgayThu, item.TenDd)).ToList();

    public async Task<Dictionary<int, string>> PlaceNamesAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        ids.Count == 0 ? [] : await db.Diadiem.AsNoTracking().Where(item => ids.Contains(item.MaDd)).ToDictionaryAsync(item => item.MaDd, item => item.TenDd, ct);

    public async Task<Dictionary<int, string>> TourNamesAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        await VisibleTours.Where(item => ids.Contains(item.MaTour)).ToDictionaryAsync(item => item.MaTour, item => item.TenTour, ct);

    public async Task<List<Destination>> DestinationsAsync(IReadOnlyCollection<int>? requested, CancellationToken ct)
    {
        var ids = requested?.Distinct().Take(Limits.MaxIds).ToList();
        var query = VisiblePlaces;
        if (ids is { Count: > 0 })
            query = query.Where(item => ids.Contains(item.MaDd));
        var all = (await query.OrderBy(item => item.MaDd).ToListAsync(ct)).Select(ToDto).ToList();
        return ids is { Count: > 0 } ? ids.Select(id => all.FirstOrDefault(item => item.Id == id)).OfType<Destination>().ToList() : all;
    }

    /// <summary>Chi tiết địa điểm đã công bố, kèm hoạt động đã công bố và các suất đã công bố từ ngày <paramref name="today"/>.</summary>
    public async Task<DestinationDetail?> DestinationDetailAsync(int id, DateOnly today, CancellationToken ct)
    {
        var place = await VisiblePlaces.FirstOrDefaultAsync(item => item.MaDd == id, ct);
        if (place is null) return null;
        var activities = await db.Hoatdong.AsNoTracking().Include(item => item.Suat)
            .Where(item => item.MaDd == id && item.TrangThai == Published).OrderBy(item => item.MaHd).ToListAsync(ct);
        var mapped = activities.Select(a => new ActivityInfo(a.MaHd, a.MaDd, a.TenHd, a.MoTa, a.ThoiLuongPhut, a.DieuKien, a.PhiThamKhao, a.TrangThai,
            a.Suat.Where(s => s.TrangThai == Published && s.Ngay >= today).OrderBy(s => s.Ngay).ThenBy(s => s.GioBatDau)
                .Select(s => new SlotInfo(s.MaSuat, s.MaHd, s.Ngay, s.GioBatDau, s.GioKetThuc, s.TrangThai)).ToList())).ToList();
        return new DestinationDetail(place.MaDd, place.TenDd, place.KhuVuc, place.LoaiHinh, place.DiaChi, place.MoTa, place.ThoiLuongPhut,
            CatalogOptions.IllustrationFor(place.KhuVuc), place.TrangThai, place.GioMoCua, place.GioDongCua, place.ViDo, place.KinhDo, mapped,
            place.AnhMinhHoa);
    }

    public async Task<(List<Destination> Items, int Total)> SearchDestinationsAsync(string? q, string? region, string? category, int skip, int take, CancellationToken ct)
    {
        var query = VisiblePlaces;
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(item => item.TenDd.Contains(q));
        if (!string.IsNullOrWhiteSpace(region)) query = query.Where(item => item.KhuVuc == region);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(item => item.LoaiHinh == category);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(item => item.MaDd).Skip(skip).Take(take).ToListAsync(ct);
        return (rows.Select(ToDto).ToList(), total);
    }

    public async Task<Destination?> SaveDestinationAsync(int? id, DestinationDraft draft, CancellationToken ct)
    {
        Diadiem? row;
        if (id is null)
        {
            row = new Diadiem { TrangThai = Published };
            db.Diadiem.Add(row);
        }
        else if ((row = await db.Diadiem.SingleOrDefaultAsync(item => item.MaDd == id, ct)) is null)
            return null;
        row.TenDd = draft.Name.Trim();
        row.KhuVuc = draft.Region;
        row.LoaiHinh = draft.Category;
        row.DiaChi = draft.Address.Trim();
        row.MoTa = draft.Description.Trim();
        row.ThoiLuongPhut = draft.VisitMinutes;
        if (draft.Status is not null) row.TrangThai = draft.Status;
        await db.SaveChangesAsync(ct);
        return ToDto(row);
    }

    public async Task<List<TourDto>> ToursAsync(CancellationToken ct) =>
        await WithPlacesAsync(await VisibleTours.OrderBy(item => item.MaTour).ToListAsync(ct), ct);

    public async Task<TourDto?> TourAsync(int id, CancellationToken ct) =>
        (await WithPlacesAsync(await VisibleTours.Where(item => item.MaTour == id).ToListAsync(ct), ct)).FirstOrDefault();

    public async Task<(List<TourDto> Items, int Total)> SearchToursAsync(string? q, string? region, string? category, string? sort, int skip, int take, CancellationToken ct)
    {
        var query = VisibleTours;
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(item => item.TenTour.Contains(q) || item.TomTat.Contains(q));
        if (!string.IsNullOrWhiteSpace(region)) query = query.Where(item => item.KhuVuc == region);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(item => item.LoaiHinh == category);
        var total = await query.CountAsync(ct);
        query = sort switch
        {
            "price-asc" => query.OrderBy(item => item.GiaThamKhao).ThenBy(item => item.MaTour),
            "price-desc" => query.OrderByDescending(item => item.GiaThamKhao).ThenBy(item => item.MaTour),
            _ => query.OrderBy(item => item.MaTour)
        };
        return (await WithPlacesAsync(await query.Skip(skip).Take(take).ToListAsync(ct), ct), total);
    }

    private async Task<List<TourDto>> WithPlacesAsync(List<TourRow> rows, CancellationToken ct)
    {
        var ids = rows.Select(item => item.MaTour).ToList();
        var links = await db.TourDiadiem.AsNoTracking().Where(link => ids.Contains(link.MaTour))
            .OrderBy(link => link.NgayThu).ThenBy(link => link.ThuTu).ToListAsync(ct);
        return rows.Select(row => new TourDto(row.MaTour, row.TenTour, row.KhuVuc, row.LoaiHinh, row.ThoiLuong, row.GiaThamKhao, row.TomTat,
            CatalogOptions.IllustrationFor(row.KhuVuc), links.Where(link => link.MaTour == row.MaTour).Select(link => link.MaDd).ToArray())).ToList();
    }

    // ---- Lịch khởi hành và giữ chỗ (UC11, UC16) ----

    public async Task<List<DepartureInfo>> DeparturesAsync(int tourId, DateTime now, CancellationToken ct) =>
        (await db.Lichkhoihanh.AsNoTracking().Include(item => item.MaTourNavigation)
            .Where(item => item.MaTour == tourId && item.TrangThai == DepartureStatus.Open && item.NgayGioKhoiHanh > now
                && item.MaTourNavigation.TrangThai == Published)
            .OrderBy(item => item.NgayGioKhoiHanh).ToListAsync(ct))
        .Select(ToInfo).ToList();

    public async Task<Dictionary<int, DepartureInfo>> DeparturesAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        (await db.Lichkhoihanh.AsNoTracking().Include(item => item.MaTourNavigation)
            .Where(item => ids.Contains(item.MaLkh)).ToListAsync(ct)).ToDictionary(item => item.MaLkh, ToInfo);

    private static DepartureInfo ToInfo(Lichkhoihanh row) => new(row.MaLkh, row.MaTour, row.MaTourNavigation.TenTour, row.NgayGioKhoiHanh, row.Gia,
        row.SucChua, row.SoChoDaGiu, Math.Max(0, row.SucChua - row.SoChoDaGiu), row.TrangThai, row.MaTourNavigation.DieuKienHuy);

    public async Task<bool> TourVisibleAsync(int tourId, CancellationToken ct) => await VisibleTours.AnyAsync(item => item.MaTour == tourId, ct);

    public async Task<List<SlotDetail>> SlotDetailsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        (await db.Suat.AsNoTracking().Where(item => ids.Contains(item.MaSuat))
            .Select(item => new { item.MaSuat, item.MaHd, item.MaHdNavigation.TenHd, item.MaHdNavigation.MaDd, item.Ngay, item.GioBatDau, item.GioKetThuc, item.TrangThai })
            .ToListAsync(ct))
        .Select(item => new SlotDetail(item.MaSuat, item.MaHd, item.TenHd, item.MaDd, item.Ngay, item.GioBatDau, item.GioKetThuc, item.TrangThai,
            (int)(item.GioKetThuc - item.GioBatDau).TotalMinutes)).ToList();

    public async Task<Dictionary<int, DestinationHours>> HoursAsync(IReadOnlyCollection<int> destinationIds, CancellationToken ct) =>
        (await db.Diadiem.AsNoTracking().Where(item => destinationIds.Contains(item.MaDd)).ToListAsync(ct))
            .ToDictionary(item => item.MaDd, item => new DestinationHours(item.GioMoCua, item.GioDongCua, item.ThoiLuongPhut));

    /// <summary>Một lệnh UPDATE có điều kiện: hai yêu cầu tranh chỗ cuối thì chỉ một dòng được cập nhật. Trả số dòng bị ảnh hưởng (0 hoặc 1).</summary>
    public Task<int> TryHoldAsync(int departureId, int guests, DateTime now, CancellationToken ct) =>
        db.Lichkhoihanh
            .Where(item => item.MaLkh == departureId && item.TrangThai == DepartureStatus.Open && item.NgayGioKhoiHanh > now
                && item.SoChoDaGiu + guests <= item.SucChua
                && db.Tour.Any(tour => tour.MaTour == item.MaTour && tour.TrangThai == Published))
            .ExecuteUpdateAsync(set => set
                .SetProperty(item => item.SoChoDaGiu, item => item.SoChoDaGiu + guests)
                .SetProperty(item => item.TrangThai, item => item.SoChoDaGiu + guests == item.SucChua ? DepartureStatus.Closed : DepartureStatus.Open), ct);

    /// <summary>Trừ chỗ đã giữ; lịch đang Closed mà hết đầy thì về Open, lịch Inactive giữ nguyên trạng thái.</summary>
    public Task<int> ReleaseAsync(int departureId, int guests, CancellationToken ct) =>
        db.Lichkhoihanh.Where(item => item.MaLkh == departureId && item.SoChoDaGiu >= guests)
            .ExecuteUpdateAsync(set => set
                .SetProperty(item => item.SoChoDaGiu, item => item.SoChoDaGiu - guests)
                .SetProperty(item => item.TrangThai, item => item.TrangThai == DepartureStatus.Closed && item.SoChoDaGiu - guests < item.SucChua
                    ? DepartureStatus.Open : item.TrangThai), ct);

    public async Task<DepartureState?> StateAsync(int departureId, CancellationToken ct) =>
        await db.Lichkhoihanh.AsNoTracking().Where(item => item.MaLkh == departureId)
            .Select(item => new DepartureState(item.TrangThai, item.SucChua, item.SoChoDaGiu, item.NgayGioKhoiHanh, item.MaTourNavigation.TrangThai == Published))
            .FirstOrDefaultAsync(ct);
}

public sealed record DepartureState(string Status, int Capacity, int Held, DateTime StartsAt, bool TourPublished)
{
    public int Remaining => Math.Max(0, Capacity - Held);
}
