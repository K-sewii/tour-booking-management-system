using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Truy vấn/ghi địa điểm cho quản trị (SCR14). Không chứa quy tắc nghiệp vụ.</summary>
public sealed class PlaceAdminRepository(DongNamTripDbContext db)
{
    private sealed record Row(Diadiem Place, int Activities, int Narrations, bool Used);

    private static IQueryable<Row> Project(IQueryable<Diadiem> query) => query.Select(x => new Row(
        x, x.Hoatdong.Count(), x.Thuyetminh.Count(), x.TourDiadiem.Any() || x.LichtrinhDiadiem.Any() || x.LichtrinhSuat.Any()));

    private static AdminDestination ToDto(Row row) => new(
        row.Place.MaDd, row.Place.TenDd, row.Place.KhuVuc, row.Place.LoaiHinh, row.Place.DiaChi, row.Place.MoTa, row.Place.ThoiLuongPhut,
        CatalogOptions.IllustrationFor(row.Place.KhuVuc), row.Place.TrangThai, row.Place.GioMoCua, row.Place.GioDongCua,
        row.Place.ViDo, row.Place.KinhDo, row.Activities, row.Narrations, row.Used);

    public async Task<List<AdminDestination>> ListAsync(CancellationToken ct) =>
        (await Project(db.Diadiem.AsNoTracking().OrderBy(x => x.MaDd)).ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<AdminDestination?> GetAsync(int id, CancellationToken ct) =>
        (await Project(db.Diadiem.AsNoTracking().Where(x => x.MaDd == id)).ToListAsync(ct)).Select(ToDto).FirstOrDefault();

    public Task<string?> StatusAsync(int id, CancellationToken ct) =>
        db.Diadiem.AsNoTracking().Where(x => x.MaDd == id).Select(x => (string?)x.TrangThai).FirstOrDefaultAsync(ct);

    public Task<bool> DuplicateAsync(int excludeId, string region, string name, CancellationToken ct) =>
        db.Diadiem.AnyAsync(x => x.MaDd != excludeId && x.KhuVuc == region && x.TenDd == name, ct);

    /// <summary>Tạo mới (<paramref name="id"/> null, trạng thái Draft) hoặc cập nhật; null nếu không tìm thấy.</summary>
    public async Task<AdminDestination?> SaveAsync(int? id, DestinationForm form, CancellationToken ct)
    {
        Diadiem? row;
        if (id is null)
        {
            row = new Diadiem { TrangThai = ContentStatus.Draft };
            db.Diadiem.Add(row);
        }
        else if ((row = await db.Diadiem.SingleOrDefaultAsync(x => x.MaDd == id, ct)) is null)
            return null;
        row.TenDd = form.Name;
        row.KhuVuc = form.Region;
        row.LoaiHinh = form.Category;
        row.DiaChi = form.Address;
        row.MoTa = form.Description;
        row.ThoiLuongPhut = form.VisitMinutes;
        row.GioMoCua = form.OpenTime;
        row.GioDongCua = form.CloseTime;
        row.ViDo = form.Latitude;
        row.KinhDo = form.Longitude;
        row.AnhMinhHoa = CatalogOptions.IllustrationFor(form.Region);
        await db.SaveChangesAsync(ct);
        return await GetAsync(row.MaDd, ct);
    }

    public async Task<bool> SetStatusAsync(int id, string status, CancellationToken ct) =>
        await db.Diadiem.Where(x => x.MaDd == id).ExecuteUpdateAsync(set => set.SetProperty(x => x.TrangThai, status), ct) > 0;

    /// <summary>Địa điểm đã có hoạt động, thuyết minh, tour hoặc lịch trình liên quan (không được xóa).</summary>
    public Task<bool> HasRelationsAsync(int id, CancellationToken ct) =>
        db.Diadiem.AnyAsync(x => x.MaDd == id
            && (x.Hoatdong.Any() || x.Thuyetminh.Any() || x.TourDiadiem.Any() || x.LichtrinhDiadiem.Any() || x.LichtrinhSuat.Any()), ct);

    /// <summary>Xóa; false nếu còn bản ghi tham chiếu (tranh chấp với giao dịch vừa phát sinh).</summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        try
        {
            return await db.Diadiem.Where(x => x.MaDd == id).ExecuteDeleteAsync(ct) > 0;
        }
        catch (Exception exception) when (DbErrors.IsForeignKeyViolation(exception))
        {
            return false;
        }
    }
}
