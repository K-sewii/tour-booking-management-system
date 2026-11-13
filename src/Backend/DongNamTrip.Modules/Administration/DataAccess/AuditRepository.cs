using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Administration;

public sealed class AuditRepository(DongNamTripDbContext db)
{
    public async Task AddAsync(int? actorId, string action, string objectType, string objectId, string? reason, string? before, string? after, CancellationToken ct)
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

    public async Task AddNoticeAsync(AccountNotice notice, CancellationToken ct)
    {
        db.AccountNotices.Add(notice);
        await db.SaveChangesAsync(ct);
    }

    public async Task<(List<AuditRow> Rows, int Total)> QueryAsync(AuditFilter filter, int page, int size, CancellationToken ct)
    {
        var query = db.Nhatkythaotac.AsNoTracking().AsQueryable();
        if (filter.From is { } from) query = query.Where(item => item.ThoiDiem >= from);
        if (filter.To is { } to) query = query.Where(item => item.ThoiDiem < to);
        if (!string.IsNullOrWhiteSpace(filter.ObjectType)) query = query.Where(item => item.LoaiDoiTuong == filter.ObjectType);
        if (filter.ActorId is { } actor) query = query.Where(item => item.MaTk == actor);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(item => item.MaNk).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return (await ToRowsAsync(rows, ct), total);
    }

    public async Task<List<AuditRow>> ByObjectAsync(string objectType, string objectId, CancellationToken ct)
    {
        var rows = await db.Nhatkythaotac.AsNoTracking()
            .Where(item => item.LoaiDoiTuong == objectType && item.MaDoiTuong == objectId)
            .OrderByDescending(item => item.MaNk).ToListAsync(ct);
        return await ToRowsAsync(rows, ct);
    }

    public async Task<List<AuditRow>> RecentByActorAsync(int actorId, int take, CancellationToken ct)
    {
        var rows = await db.Nhatkythaotac.AsNoTracking().Where(item => item.MaTk == actorId)
            .OrderByDescending(item => item.MaNk).Take(take).ToListAsync(ct);
        return await ToRowsAsync(rows, ct);
    }

    public async Task<Dictionary<int, string>> NamesAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        await db.Taikhoan.AsNoTracking().Where(item => ids.Contains(item.MaTk)).ToDictionaryAsync(item => item.MaTk, item => item.HoTen, ct);

    private async Task<List<AuditRow>> ToRowsAsync(List<Nhatkythaotac> rows, CancellationToken ct)
    {
        var names = await NamesAsync(rows.Where(item => item.MaTk is not null).Select(item => item.MaTk!.Value).Distinct().ToArray(), ct);
        return rows.Select(item => new AuditRow(item.MaNk, item.MaTk,
            item.MaTk is { } id && names.TryGetValue(id, out var name) ? name : "Hệ thống",
            item.ThoiDiem, item.HanhDong, item.LoaiDoiTuong, item.MaDoiTuong ?? "", item.LyDo, item.DuLieuTruoc, item.DuLieuSau)).ToList();
    }
}
