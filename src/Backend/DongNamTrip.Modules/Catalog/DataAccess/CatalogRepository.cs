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
    public const string Published = "Published";

    private IQueryable<Diadiem> VisiblePlaces => db.Diadiem.AsNoTracking().Where(item => item.TrangThai == Published);
    private IQueryable<TourRow> VisibleTours => db.Tour.AsNoTracking().Where(item => item.TrangThai == Published);

    private static Destination ToDto(Diadiem row) =>
        new(row.MaDd, row.TenDd, row.KhuVuc, row.LoaiHinh, row.DiaChi, row.MoTa, row.ThoiLuongPhut, CatalogOptions.IllustrationFor(row.KhuVuc), row.TrangThai);

    public async Task<List<Destination>> AdminDestinationsAsync(CancellationToken ct) =>
        (await db.Diadiem.AsNoTracking().OrderBy(item => item.MaDd).ToListAsync(ct)).Select(ToDto).ToList();

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
}
