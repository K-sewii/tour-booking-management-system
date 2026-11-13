using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using TourRow = DongNamTrip.Modules.Persistence.Entities.Tour;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Truy cập dữ liệu quản trị tour và lịch khởi hành (UC16).</summary>
public sealed class TourAdminRepository(DongNamTripDbContext db)
{
    private const string Collation = "Vietnamese_CI_AI";

    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginAsync(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);

    public async Task<List<AdminTour>> ListAsync(string? status, CancellationToken ct)
    {
        var query = db.Tour.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.TrangThai == status);
        return await query.OrderBy(item => item.MaTour).Select(item => new AdminTour(item.MaTour, item.TenTour, item.KhuVuc, item.LoaiHinh, item.ThoiLuong,
            item.GiaThamKhao, item.TrangThai, item.TourDiadiem.Count, item.Lichkhoihanh.Count(l => l.TrangThai == DepartureStatus.Open))).ToListAsync(ct);
    }

    public async Task<AdminTour?> TourAsync(int id, CancellationToken ct) =>
        await db.Tour.AsNoTracking().Where(item => item.MaTour == id).Select(item => new AdminTour(item.MaTour, item.TenTour, item.KhuVuc, item.LoaiHinh, item.ThoiLuong,
            item.GiaThamKhao, item.TrangThai, item.TourDiadiem.Count, item.Lichkhoihanh.Count(l => l.TrangThai == DepartureStatus.Open))).FirstOrDefaultAsync(ct);

    public async Task<AdminTourDetail?> GetAsync(int id, CancellationToken ct)
    {
        var row = await db.Tour.AsNoTracking().FirstOrDefaultAsync(item => item.MaTour == id, ct);
        if (row is null) return null;
        var stops = await db.TourDiadiem.AsNoTracking().Where(item => item.MaTour == id).OrderBy(item => item.NgayThu).ThenBy(item => item.ThuTu)
            .Select(item => new TourStop(item.MaDd, item.MaDdNavigation.TenDd, item.NgayThu, item.ThuTu)).ToListAsync(ct);
        return new AdminTourDetail((await TourAsync(id, ct))!, row.TomTat, row.DieuKienHuy, stops, await DeparturesAsync(id, ct));
    }

    public Task<string?> StatusAsync(int id, CancellationToken ct) =>
        db.Tour.AsNoTracking().Where(item => item.MaTour == id).Select(item => item.TrangThai).Cast<string?>().FirstOrDefaultAsync(ct);

    public async Task<TourForm?> FormAsync(int id, CancellationToken ct) =>
        await db.Tour.AsNoTracking().Where(item => item.MaTour == id)
            .Select(item => new TourForm(item.TenTour, item.KhuVuc, item.LoaiHinh, item.ThoiLuong, item.GiaThamKhao, item.TomTat, item.DieuKienHuy)).FirstOrDefaultAsync(ct);

    /// <summary>Trùng (tên, khu vực) không phân biệt hoa thường và dấu; CSDL chưa có ràng buộc duy nhất.</summary>
    public Task<bool> NameExistsAsync(string name, string region, int? exceptId, CancellationToken ct) =>
        db.Tour.AsNoTracking().AnyAsync(item => item.MaTour != exceptId && item.KhuVuc == region
            && EF.Functions.Collate(item.TenTour, Collation) == name, ct);

    public async Task<AdminTour?> SaveAsync(int? id, TourForm form, CancellationToken ct)
    {
        TourRow? row;
        if (id is null)
        {
            row = new TourRow { TrangThai = TourStatus.Draft };
            db.Tour.Add(row);
        }
        else if ((row = await db.Tour.SingleOrDefaultAsync(item => item.MaTour == id, ct)) is null)
            return null;
        row.TenTour = form.Name;
        row.KhuVuc = form.Region;
        row.LoaiHinh = form.Category;
        row.ThoiLuong = form.Duration;
        row.GiaThamKhao = form.ReferencePrice;
        row.TomTat = form.Summary;
        row.DieuKienHuy = string.IsNullOrWhiteSpace(form.CancelPolicy) ? null : form.CancelPolicy;
        await db.SaveChangesAsync(ct);
        return await TourAsync(row.MaTour, ct);
    }

    public async Task<List<int>> PublishedPlaceIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        await db.Diadiem.AsNoTracking().Where(item => ids.Contains(item.MaDd) && item.TrangThai == ContentStatus.Published).Select(item => item.MaDd).ToListAsync(ct);

    /// <summary>Xóa hết rồi thêm; gọi trong một giao dịch.</summary>
    public async Task ReplacePlacesAsync(int id, IReadOnlyList<TourStopForm> stops, CancellationToken ct)
    {
        await db.TourDiadiem.Where(item => item.MaTour == id).ExecuteDeleteAsync(ct);
        db.TourDiadiem.AddRange(stops.Select(stop => new TourDiadiem { MaTour = id, MaDd = stop.DestinationId, NgayThu = stop.Day, ThuTu = stop.Order }));
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> SetStatusAsync(int id, string status, CancellationToken ct) =>
        await db.Tour.Where(item => item.MaTour == id).ExecuteUpdateAsync(set => set.SetProperty(item => item.TrangThai, status), ct) == 1;

    public Task<bool> HasHeldSeatsAsync(int tourId, CancellationToken ct) =>
        db.Lichkhoihanh.AnyAsync(item => item.MaTour == tourId && item.SoChoDaGiu > 0, ct);

    public Task<int> StopCountAsync(int tourId, CancellationToken ct) => db.TourDiadiem.CountAsync(item => item.MaTour == tourId, ct);

    // ---- Lịch khởi hành ----

    public async Task<List<AdminDeparture>> DeparturesAsync(int tourId, CancellationToken ct) =>
        await db.Lichkhoihanh.AsNoTracking().Where(item => item.MaTour == tourId).OrderBy(item => item.NgayGioKhoiHanh)
            .Select(item => new AdminDeparture(item.MaLkh, item.MaTour, item.NgayGioKhoiHanh, item.Gia, item.SucChua, item.SoChoDaGiu, item.TrangThai)).ToListAsync(ct);

    public async Task<AdminDeparture?> DepartureAsync(int id, CancellationToken ct) =>
        await db.Lichkhoihanh.AsNoTracking().Where(item => item.MaLkh == id)
            .Select(item => new AdminDeparture(item.MaLkh, item.MaTour, item.NgayGioKhoiHanh, item.Gia, item.SucChua, item.SoChoDaGiu, item.TrangThai)).FirstOrDefaultAsync(ct);

    /// <summary>Tạo (<paramref name="id"/> null, trạng thái Open) hoặc sửa; sửa thì tính lại Open/Closed trừ lịch Inactive.</summary>
    public async Task<AdminDeparture?> SaveDepartureAsync(int tourId, int? id, DepartureForm form, CancellationToken ct)
    {
        Lichkhoihanh? row;
        if (id is null)
        {
            row = new Lichkhoihanh { MaTour = tourId, SoChoDaGiu = 0, TrangThai = DepartureStatus.Open };
            db.Lichkhoihanh.Add(row);
        }
        else if ((row = await db.Lichkhoihanh.SingleOrDefaultAsync(item => item.MaLkh == id, ct)) is null)
            return null;
        row.NgayGioKhoiHanh = form.StartsAt;
        row.Gia = form.Price;
        row.SucChua = form.Capacity;
        if (row.TrangThai != DepartureStatus.Inactive)
            row.TrangThai = row.SoChoDaGiu >= row.SucChua ? DepartureStatus.Closed : DepartureStatus.Open;
        await db.SaveChangesAsync(ct);
        return await DepartureAsync(row.MaLkh, ct);
    }

    public async Task<bool> SetDepartureStatusAsync(int id, string status, CancellationToken ct) =>
        await db.Lichkhoihanh.Where(item => item.MaLkh == id).ExecuteUpdateAsync(set => set.SetProperty(item => item.TrangThai, status), ct) == 1;
}
