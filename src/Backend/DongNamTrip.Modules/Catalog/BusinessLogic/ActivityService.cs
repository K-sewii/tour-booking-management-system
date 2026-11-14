using DongNamTrip.Contracts;
using DongNamTrip.Modules.Administration;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Hoạt động và suất (UC15, SCR15): kiểm tra ngày giờ, công bố/tạm ngừng, cảnh báo lịch trình bị ảnh hưởng.</summary>
public sealed class ActivityService(ActivityRepository repository, IAdministrationApi administration)
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public Task<List<AdminActivity>> ListAsync(int? destinationId, CancellationToken ct) => repository.ListAsync(destinationId, ct);

    public async Task<OpResult<AdminActivity>> SaveAsync(int? id, ActivityForm form, int actor, CancellationToken ct)
    {
        form.Name = form.Name.Trim();
        form.Description = Blank(form.Description);
        form.Conditions = Blank(form.Conditions);
        if (form.Name.Length == 0)
            return Op.Invalid("Vui lòng nhập tên hoạt động.");
        if (form.ReferenceFee is { } fee && fee != decimal.Truncate(fee))
            return Op.Invalid("Phí tham khảo phải là số tiền nguyên (₫).");
        if (!await repository.PlaceExistsAsync(form.DestinationId, ct))
            return Op.Invalid("Địa điểm không tồn tại.");
        if (await repository.DuplicateNameAsync(id ?? 0, form.DestinationId, form.Name, ct))
            return Op.Conflict("Địa điểm này đã có hoạt động cùng tên.");
        AdminActivity? before = null;
        if (id is { } existing)
        {
            before = await repository.GetAsync(existing, ct);
            if (before is null)
                return Op.NotFound("Không tìm thấy hoạt động.");
            if (before.DestinationId != form.DestinationId && await repository.InSchedulesAsync(existing, ct))
                return Op.Conflict("Hoạt động đã nằm trong lịch trình của khách nên không thể chuyển sang địa điểm khác.");
        }
        var saved = await repository.SaveAsync(id, form, ct);
        if (saved is null)
            return Op.NotFound("Không tìm thấy hoạt động.");
        await administration.RecordAsync(new AuditEntry(actor, id is null ? AuditActions.ActivityCreated : AuditActions.ActivityEdited, "HOATDONG", saved.Id, null, before, saved), ct);
        return Op.Ok(saved);
    }

    public async Task<OpResult<ImpactResult>> SetActivityStatusAsync(int id, string status, bool confirm, int actor, CancellationToken ct)
    {
        var context = await repository.ContextAsync(id, ct);
        if (context is null)
            return Op.NotFound("Không tìm thấy hoạt động.");
        var allowed = (context.Status, status) is (ContentStatus.Draft, ContentStatus.Published)
            or (ContentStatus.Published, ContentStatus.Inactive)
            or (ContentStatus.Inactive, ContentStatus.Published);
        if (!allowed)
            return Op.Invalid($"Không thể chuyển hoạt động từ \"{ContentStatus.Label(context.Status)}\" sang \"{ContentStatus.Label(status)}\".");
        if (status == ContentStatus.Published && context.DestinationStatus != ContentStatus.Published)
            return Op.Conflict("Hãy công bố địa điểm trước khi công bố hoạt động.");
        var affected = status == ContentStatus.Inactive ? await repository.AffectedByActivityAsync(id, ct) : 0;
        if (affected > 0 && !confirm)
            return Confirm($"Có {affected} lịch trình của khách đang dùng suất của hoạt động này. Bạn vẫn muốn tạm ngừng?", affected);
        var before = await repository.GetAsync(id, ct);
        await repository.SetStatusAsync(id, status, ct);
        var after = await repository.GetAsync(id, ct);
        await administration.RecordAsync(new AuditEntry(actor, status == ContentStatus.Published ? AuditActions.ActivityPublished : AuditActions.ActivityStopped, "HOATDONG", id,
            affected > 0 ? $"Ảnh hưởng {affected} lịch trình" : null, before, after), ct);
        return Done(affected);
    }

    public async Task<OpResult<SlotInfo>> CreateSlotAsync(int activityId, SlotForm form, int actor, CancellationToken ct)
    {
        var context = await repository.ContextAsync(activityId, ct);
        if (context is null)
            return Op.NotFound("Không tìm thấy hoạt động.");
        if (form.Date < Today)
            return Op.Invalid("Ngày tổ chức không được ở quá khứ.");
        return await SaveSlotAsync(null, context, form, actor, ct);
    }

    public async Task<OpResult<SlotInfo>> UpdateSlotAsync(int slotId, SlotForm form, int actor, CancellationToken ct)
    {
        var slot = await repository.SlotAsync(slotId, ct);
        if (slot is null)
            return Op.NotFound("Không tìm thấy suất.");
        var context = (await repository.ContextAsync(slot.ActivityId, ct))!;
        var timeChanged = slot.Date != form.Date || slot.Start != form.Start || slot.End != form.End;
        if (timeChanged)
        {
            var used = await repository.AffectedBySlotAsync(slotId, ct);
            if (used > 0)
                return Op.Conflict($"Suất đang nằm trong {used} lịch trình của khách. Hãy tạm ngừng suất này và tạo suất mới thay vì đổi giờ.");
        }
        // Chỉ chặn ngày quá khứ khi đổi ngày; sửa suất cũ mà giữ nguyên ngày thì không bị chặn.
        if (slot.Date != form.Date && form.Date < Today)
            return Op.Invalid("Ngày tổ chức không được ở quá khứ.");
        return await SaveSlotAsync(slot, context, form, actor, ct);
    }

    public async Task<OpResult<ImpactResult>> SetSlotStatusAsync(int id, string status, bool confirm, int actor, CancellationToken ct)
    {
        var slot = await repository.SlotAsync(id, ct);
        if (slot is null)
            return Op.NotFound("Không tìm thấy suất.");
        if (status is not (ContentStatus.Published or ContentStatus.Inactive) || slot.Status == status)
            return Op.Invalid("Trạng thái suất không hợp lệ.");
        if (status == ContentStatus.Published && slot.Date < Today)
            return Op.Conflict("Suất đã qua ngày tổ chức nên không công bố lại được.");
        var affected = status == ContentStatus.Inactive ? await repository.AffectedBySlotAsync(id, ct) : 0;
        if (affected > 0 && !confirm)
            return Confirm($"Có {affected} lịch trình của khách đang chọn suất này. Bạn vẫn muốn tạm ngừng?", affected);
        await repository.SetSlotStatusAsync(id, status, ct);
        await administration.RecordAsync(new AuditEntry(actor, status == ContentStatus.Published ? AuditActions.SlotPublished : AuditActions.SlotStopped, "SUAT", id,
            affected > 0 ? $"Ảnh hưởng {affected} lịch trình" : null, slot, slot with { Status = status }), ct);
        return Done(affected);
    }

    private async Task<OpResult<SlotInfo>> SaveSlotAsync(SlotState? existing, ActivityContext activity, SlotForm form, int actor, CancellationToken ct)
    {
        if (form.End <= form.Start)
            return Op.Invalid("Giờ kết thúc phải sau giờ bắt đầu.");
        if ((form.End.ToTimeSpan() - form.Start.ToTimeSpan()).TotalMinutes < activity.DurationMinutes)
            return Op.Invalid($"Suất ngắn hơn thời lượng hoạt động ({activity.DurationMinutes} phút).");
        if (activity.OpenTime is { } open && form.Start < open || activity.CloseTime is { } close && form.End > close)
            return Op.Invalid("Suất nằm ngoài giờ mở cửa của địa điểm.");
        var others = await repository.RangesAsync(activity.Id, form.Date, existing?.Id ?? 0, ct);
        if (others.Any(range => form.Start < range.End && range.Start < form.End))
            return Op.Conflict("Suất bị trùng giờ với một suất khác của hoạt động này.");
        var saved = await repository.SaveSlotAsync(existing?.Id, activity.Id, form, ct);
        if (saved.Duplicate)
            return Op.Conflict("Suất bị trùng giờ với một suất khác của hoạt động này.");
        if (saved.Slot is null)
            return Op.NotFound("Không tìm thấy suất.");
        await administration.RecordAsync(new AuditEntry(actor, existing is null ? AuditActions.SlotCreated : AuditActions.SlotEdited, "SUAT", saved.Slot.Id, null, existing, saved.Slot), ct);
        return Op.Ok(saved.Slot);
    }

    private static OpResult<ImpactResult> Confirm(string message, int affected) =>
        Op.Conflict(message, new ImpactResult(ImpactStatuses.NeedConfirm, affected, message));

    private static OpResult<ImpactResult> Done(int affected) => Op.Ok(new ImpactResult(ImpactStatuses.Done, affected,
        affected > 0 ? $"Đã cập nhật. {affected} lịch trình bị ảnh hưởng cần được kiểm tra lại." : "Đã cập nhật."));

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
