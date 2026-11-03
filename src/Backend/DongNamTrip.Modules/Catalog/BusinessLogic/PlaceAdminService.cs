using DongNamTrip.Contracts;
using DongNamTrip.Modules.Administration;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Quản trị địa điểm (UC14, SCR14): thêm/sửa, công bố/ngừng công bố, xóa bản nháp, ghi nhật ký thao tác.</summary>
public sealed class PlaceAdminService(PlaceAdminRepository repository, IAdministrationApi administration)
{
    private const string ObjectType = "DIADIEM";

    public Task<List<AdminDestination>> ListAsync(CancellationToken ct) => repository.ListAsync(ct);

    public async Task<List<DestinationOption>> OptionsAsync(CancellationToken ct) =>
        (await repository.ListAsync(ct)).Select(item => new DestinationOption(item.Id, item.Name, item.Status)).ToList();

    public async Task<OpResult<AdminDestination>> SaveAsync(int? id, DestinationForm form, int actor, CancellationToken ct)
    {
        form.Name = form.Name.Trim();
        form.Address = form.Address.Trim();
        form.Description = form.Description.Trim();
        if (await repository.DuplicateAsync(id ?? 0, form.Region, form.Name, ct))
            return Op.Conflict("Địa điểm này đã có trong khu vực đã chọn.");
        var before = id is null ? null : await repository.GetAsync(id.Value, ct);
        if (id is not null && before is null)
            return Op.NotFound("Không tìm thấy địa điểm.");
        var saved = await repository.SaveAsync(id, form, ct);
        if (saved is null)
            return Op.NotFound("Không tìm thấy địa điểm.");
        await administration.RecordAsync(new AuditEntry(actor, id is null ? "Tạo địa điểm" : "Sửa địa điểm", ObjectType, saved.Id, null, before, saved), ct);
        return Op.Ok(saved);
    }

    public async Task<OpResult<AdminDestination>> SetStatusAsync(int id, string status, int actor, CancellationToken ct)
    {
        var before = await repository.GetAsync(id, ct);
        if (before is null)
            return Op.NotFound("Không tìm thấy địa điểm.");
        var allowed = (before.Status, status) is (ContentStatus.Draft, ContentStatus.Published)
            or (ContentStatus.Published, ContentStatus.Inactive)
            or (ContentStatus.Inactive, ContentStatus.Published);
        if (!allowed)
            return Op.Invalid($"Không thể chuyển địa điểm từ \"{ContentStatus.Label(before.Status)}\" sang \"{ContentStatus.Label(status)}\".");
        await repository.SetStatusAsync(id, status, ct);
        var after = (await repository.GetAsync(id, ct))!;
        await administration.RecordAsync(new AuditEntry(actor, status == ContentStatus.Published ? "Công bố địa điểm" : "Ngừng công bố địa điểm", ObjectType, id, null, before, after), ct);
        return Op.Ok(after);
    }

    /// <summary>Chỉ xóa bản nháp chưa có liên quan; địa điểm đã phát sinh dữ liệu phải Ngừng công bố để giữ lịch sử.</summary>
    public async Task<OpResult<DeleteResult>> DeleteAsync(int id, int actor, CancellationToken ct)
    {
        var before = await repository.GetAsync(id, ct);
        if (before is null)
            return Op.NotFound("Không tìm thấy địa điểm.");
        if (before.Status != ContentStatus.Draft)
            return Refuse("Chỉ xóa được bản nháp. Hãy chọn \"Ngừng công bố\" để giữ lại lịch sử và dữ liệu liên quan.");
        if (await repository.HasRelationsAsync(id, ct) || !await repository.DeleteAsync(id, ct))
            return Refuse("Địa điểm đã có hoạt động, thuyết minh, tour hoặc lịch trình liên quan nên không xóa được. Hãy ngừng công bố thay vì xóa.");
        await administration.RecordAsync(new AuditEntry(actor, "Xóa bản nháp địa điểm", ObjectType, id, null, before, null), ct);
        return Op.Ok(new DeleteResult(true, "Đã xóa bản nháp."));
    }

    private static OpResult<DeleteResult> Refuse(string message) => Op.Conflict(message, new DeleteResult(false, message));
}
