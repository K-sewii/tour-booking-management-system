using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Administration;

public sealed class AuditRepository(DongNamTripDbContext db)
{
    // These actions are emitted by staff-authorized endpoints; role snapshots do not exist for other historic rows.
    private static readonly string[] StaffAuditActions = [
        AuditActions.AccountRoleChanged, AuditActions.AccountStatusChangedLegacy, AuditActions.AccountLocked, AuditActions.AccountUnlocked, AuditActions.AccountStaffCreated, AuditActions.AccountPasswordReset,
        AuditActions.TourCreated, AuditActions.TourEdited, AuditActions.TourStopsEdited, AuditActions.TourPublished, AuditActions.TourStopped, AuditActions.DepartureCreated, AuditActions.DepartureEdited, AuditActions.DepartureStopped,
        AuditActions.OrderConfirmed, AuditActions.OrderAdjusted, AuditActions.OrderCancellationApproved, AuditActions.OrderCancellationRejected, AuditActions.ReceiptRecorded, AuditActions.RefundRecorded, AuditActions.OrderCompleted,
        AuditActions.RequestReceived, AuditActions.VerificationRecorded, AuditActions.QuoteIssued, AuditActions.PrivateRequestCancelled, AuditActions.DestinationCreated, AuditActions.DestinationEdited, AuditActions.DestinationPublished, AuditActions.DestinationUnpublished, AuditActions.DestinationDraftDeleted,
        AuditActions.AudioFailed, AuditActions.AudioCreated, AuditActions.AudioPublished, AuditActions.AudioUnpublished, AuditActions.NarrationCreated, AuditActions.NarrationEdited, AuditActions.NarrationVersionCreated, AuditActions.TranslationAdded, AuditActions.TranslationEdited, AuditActions.ListenRightGranted, AuditActions.ListenRightRevoked,
        AuditActions.ActivityCreated, AuditActions.ActivityEdited, AuditActions.ActivityPublished, AuditActions.ActivityStopped, AuditActions.SlotCreated, AuditActions.SlotEdited, AuditActions.SlotPublished, AuditActions.SlotStopped
    ];
    private static readonly string[] CustomerAuditActions = [AuditActions.QuoteAccepted, AuditActions.QuoteChangeRequested];
    public async Task<AdminAuditPage> AuditAsync(int page, string? actorType, DateTime? from, DateTime? to, string? query, CancellationToken ct)
    {
        // These account mutations are written only by the Admin-protected account endpoints.
        // Other historic entries have no role snapshot; current account roles are not evidence.
        var source = db.Nhatkythaotac.AsNoTracking();
        if (actorType == "staff") source = source.Where(x => x.MaTk != null && (StaffAuditActions.Contains(x.HanhDong) || (x.LoaiDoiTuong == "THUYETMINH" && (x.HanhDong.StartsWith("Thuyết minh → ") || x.HanhDong.StartsWith("Bản dịch → ")))) && (x.HanhDong != AuditActions.AccountStatusChangedLegacy || x.LoaiDoiTuong == "TAIKHOAN"));
        else if (actorType == "system") source = source.Where(x => x.MaTk == null);
        else if (actorType == "customer") source = source.Where(x => x.MaTk != null && CustomerAuditActions.Contains(x.HanhDong));
        if (!string.IsNullOrWhiteSpace(query)) { var term = query.Trim(); source = source.Where(x => x.HanhDong.Contains(term) || (x.MaTkNavigation != null && x.MaTkNavigation.HoTen.Contains(term))); }
        if (from is { } start) source = source.Where(x => x.ThoiDiem >= start);
        if (to is { } end) source = source.Where(x => x.ThoiDiem < end);
        var count = await source.CountAsync(ct);
        var rows = await source.OrderByDescending(x => x.ThoiDiem).ThenByDescending(x => x.MaNk).Skip((page - 1) * 50).Take(51)
            .Select(x => new AdminAuditRow(x.MaNk, x.ThoiDiem, x.MaTkNavigation == null ? (x.MaTk == null ? "Hệ thống" : "Không xác định") : x.MaTkNavigation.HoTen, x.HanhDong, x.LoaiDoiTuong, x.MaDoiTuong, x.LyDo, x.LoaiDoiTuong == "TAIKHOAN" && x.HanhDong == AuditActions.AccountRoleChanged ? x.DuLieuTruoc : null, x.LoaiDoiTuong == "TAIKHOAN" && x.HanhDong == AuditActions.AccountRoleChanged ? x.DuLieuSau : null,
                x.MaTk != null && (StaffAuditActions.Contains(x.HanhDong) || (x.LoaiDoiTuong == "THUYETMINH" && (x.HanhDong.StartsWith("Thuyết minh → ") || x.HanhDong.StartsWith("Bản dịch → ")))) && (x.HanhDong != AuditActions.AccountStatusChangedLegacy || x.LoaiDoiTuong == "TAIKHOAN") ? "staff" : x.MaTk == null ? "system" : x.MaTk != null && CustomerAuditActions.Contains(x.HanhDong) ? "customer" : "unknown",
                x.LoaiDoiTuong == "TEPAUDIO" && x.HanhDong == AuditActions.AudioFailed ? "Tạo audio thất bại" : null)).ToListAsync(ct);
        return new AdminAuditPage(rows.Take(50).ToList(), rows.Count > 50, Math.Max(1, (count + 49) / 50), count);
    }

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
            item.MaTk is { } id ? (names.TryGetValue(id, out var name) ? name : "Không xác định") : "Hệ thống",
            item.ThoiDiem, item.HanhDong, item.LoaiDoiTuong, item.MaDoiTuong ?? "", item.LyDo, item.DuLieuTruoc, item.DuLieuSau)).ToList();
    }
}
