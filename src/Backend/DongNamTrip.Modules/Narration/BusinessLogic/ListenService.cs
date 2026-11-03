using DongNamTrip.Contracts;
using DongNamTrip.Modules.Administration;

namespace DongNamTrip.Modules.Narration;

/// <summary>Nghe thuyết minh và quyền nghe (UC23, SCR11).</summary>
public sealed class ListenService(ListenRepository repository, AudioFileStore files, IAdministrationApi administration) : INarrationApi
{
    private const string NoRight = "Bạn cần có đơn tour đã được xác nhận để nghe thuyết minh.";
    private const string Expired = "Quyền nghe thuyết minh của bạn đã hết hạn.";

    public async Task<ListenCatalog> CatalogAsync(int accountId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var until = await repository.ActiveUntilAsync(accountId, now, ct);
        if (until.Count == 0)
            return new ListenCatalog(false, null, await repository.EverHadRightAsync(accountId, ct) ? Expired : NoRight, []);
        var destinations = await repository.AllowedDestinationsAsync(accountId, now, ct);
        var tracks = destinations.Count == 0 ? [] : await repository.TracksAsync(destinations, ct);
        return new ListenCatalog(true, until.Max(), tracks.Count == 0 ? "Chuyến đi của bạn chưa có thuyết minh được công bố." : "",
            tracks.OrderBy(x => x.DestinationId).ThenBy(x => x.LanguageCode).ToList());
    }

    /// <summary>Kiểm tra quyền truy cập tệp: thuyết minh đã công bố, đơn còn hạn nghe và địa điểm thuộc chuyến đi.</summary>
    public async Task<OpResult<string>> FileAsync(int audioId, int accountId, CancellationToken ct)
    {
        var track = await repository.TrackAsync(audioId, ct);
        if (track is null)
            return Op.NotFound("Không tìm thấy thuyết minh được công bố.");
        var allowed = await repository.AllowedDestinationsAsync(accountId, DateTime.UtcNow, ct);
        if (!allowed.Contains(track.DestinationId))
            return Op.Forbidden(await repository.EverHadRightAsync(accountId, ct) ? "Quyền nghe đã hết hạn hoặc không bao gồm địa điểm này." : NoRight);
        var path = files.Find(await repository.FileNameAsync(audioId, ct));
        return path is null ? Op.NotFound("Tệp audio không còn tồn tại.") : Op.Ok(path);
    }

    public async Task<OpResult<ListenRightGrant>> GrantListenRightAsync(ListenRightGrant grant, int actorId, CancellationToken ct)
    {
        if (grant.To <= grant.From)
            return Op.Invalid("Thời hạn nghe không hợp lệ.");
        var owner = await repository.OrderOwnerAsync(grant.OrderId, ct);
        if (owner is null)
            return Op.NotFound("Không tìm thấy đơn đặt.");
        if (owner != grant.AccountId)
            return Op.Invalid("Đơn đặt không thuộc tài khoản này.");
        var saved = await repository.SaveRightAsync(grant, ct);
        await administration.RecordAsync(new AuditEntry(actorId, "Cấp quyền nghe", "QUYENNGHE", saved.Id, null, saved.Before, grant), ct);
        return Op.Ok(grant);
    }

    public async Task RevokeListenRightAsync(int orderId, int actorId, CancellationToken ct)
    {
        foreach (var removed in await repository.RemoveRightsAsync(orderId, ct))
            await administration.RecordAsync(new AuditEntry(actorId, "Thu hồi quyền nghe", "QUYENNGHE", orderId, null, removed, null), ct);
    }
}
