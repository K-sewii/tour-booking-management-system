using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;

namespace DongNamTrip.Modules.Administration;

public sealed class AuditRepository(DongNamTripDbContext db)
{
    public async Task AddAsync(int actorId, string action, string objectType, string objectId, string? reason, string? before, string? after, CancellationToken ct)
    {
        db.Nhatkythaotac.Add(new Nhatkythaotac
        {
            MaTk = actorId,
            ThoiDiem = DateTime.UtcNow,
            HanhDong = action,
            LoaiDoiTuong = objectType,
            MaDoiTuong = objectId,
            LyDo = reason,
            DuLieuTruoc = before,
            DuLieuSau = after
        });
        await db.SaveChangesAsync(ct);
    }
}
