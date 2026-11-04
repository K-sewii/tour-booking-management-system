using DongNamTrip.Contracts;
using DongNamTrip.Modules.Administration;
using DongNamTrip.Modules.Catalog;

namespace DongNamTrip.Modules.Narration;

/// <summary>
/// Biên tập thuyết minh, bản dịch và audio (UC21, UC22, SCR20). Biên tập (soạn, dịch, tạo audio) thuộc Editor/Admin;
/// duyệt và công bố cần <c>canPublish</c> (Admin/Publisher).
/// </summary>
public sealed class NarrationService(NarrationRepository repository, AudioFileStore files, ICatalogApi catalog, IAdministrationApi administration)
{
    private const long MaxAudioBytes = 20 * 1024 * 1024;
    private const string NoPermission = "Chỉ người được cấp quyền duyệt/công bố mới thực hiện được thao tác này.";

    public Task<List<LanguageInfo>> LanguagesAsync(CancellationToken ct) => repository.LanguagesAsync(ct);

    public Task<List<NarrationInfo>> ListAsync(CancellationToken ct) => repository.ListAsync(ct);

    public async Task<OpResult<NarrationInfo>> SaveAsync(int? id, NarrationForm form, int actor, CancellationToken ct)
    {
        form.Title = form.Title.Trim();
        form.Source = form.Source.Trim();
        if (form.Title.Length == 0 || form.Source.Length == 0)
            return Op.Invalid("Vui lòng nhập tiêu đề và nội dung thuyết minh.");

        int narrationId;
        NarrationInfo? before = null;
        if (id is null)
        {
            if (!await catalog.PlaceExistsAsync(form.DestinationId, ct))
                return Op.Invalid("Địa điểm không tồn tại.");
            var version = await repository.LatestVersionAsync(form.DestinationId, form.Title, ct) + 1;
            narrationId = await repository.CreateAsync(form.DestinationId, form.Title, form.Source, version, ct);
        }
        else
        {
            narrationId = id.Value;
            var head = await repository.HeadAsync(narrationId, ct);
            if (head is null)
                return Op.NotFound("Không tìm thấy thuyết minh.");
            if (head.Status is not (ContentStatus.Draft or ContentStatus.Pending))
                return Op.Conflict("Thuyết minh đã duyệt hoặc công bố không sửa trực tiếp. Hãy tạo phiên bản mới.");
            if (form.Title != head.Title && await repository.TitleExistsAsync(head.DestinationId, form.Title, narrationId, ct))
                return Op.Conflict("Địa điểm đã có thuyết minh cùng tiêu đề. Hãy dùng phiên bản mới.");
            before = await repository.GetAsync(narrationId, ct);
            await repository.UpdateAsync(narrationId, form.Title, form.Source, ct);
        }
        return await FinishAsync(narrationId, actor, id is null ? "Tạo thuyết minh" : "Sửa thuyết minh", before, null, ct);
    }

    public async Task<OpResult<NarrationInfo>> NewVersionAsync(int id, string source, int actor, CancellationToken ct)
    {
        source = source.Trim();
        if (source.Length == 0)
            return Op.Invalid("Vui lòng nhập nội dung thuyết minh.");
        var current = await repository.HeadAsync(id, ct);
        if (current is null)
            return Op.NotFound("Không tìm thấy thuyết minh.");
        var version = await repository.LatestVersionAsync(current.DestinationId, current.Title, ct) + 1;
        var created = await repository.CreateAsync(current.DestinationId, current.Title, source, version, ct);
        return await FinishAsync(created, actor, "Tạo phiên bản thuyết minh", null, $"Từ phiên bản {current.Version}", ct);
    }

    public async Task<OpResult<NarrationInfo>> SetStatusAsync(int id, string status, bool canPublish, int actor, CancellationToken ct)
    {
        var head = await repository.HeadAsync(id, ct);
        if (head is null)
            return Op.NotFound("Không tìm thấy thuyết minh.");
        var ok = (head.Status, status) is (ContentStatus.Draft, ContentStatus.Pending) or (ContentStatus.Pending, ContentStatus.Approved)
            or (ContentStatus.Pending, ContentStatus.Draft) or (ContentStatus.Approved, ContentStatus.Draft)
            or (ContentStatus.Approved, ContentStatus.Published) or (ContentStatus.Published, ContentStatus.Inactive)
            or (ContentStatus.Inactive, ContentStatus.Published);
        if (!ok)
            return Op.Invalid($"Không thể chuyển thuyết minh từ \"{ContentStatus.Label(head.Status)}\" sang \"{ContentStatus.Label(status)}\".");
        if (status is ContentStatus.Approved or ContentStatus.Published or ContentStatus.Inactive && !canPublish)
            return Op.Forbidden(NoPermission);
        if (status == ContentStatus.Pending && head.TranslationCount == 0)
            return Op.Conflict("Hãy thêm ít nhất một bản dịch trước khi gửi duyệt.");
        var before = await repository.GetAsync(id, ct);
        await repository.SetStatusAsync(id, status, status == ContentStatus.Published, ct);
        return await FinishAsync(id, actor, $"Thuyết minh → {ContentStatus.Label(status)}", before, null, ct);
    }

    public async Task<OpResult<NarrationInfo>> SaveTranslationAsync(int narrationId, TranslationForm form, int actor, CancellationToken ct)
    {
        form.Content = form.Content.Trim();
        if (form.Content.Length == 0)
            return Op.Invalid("Vui lòng nhập nội dung bản dịch.");
        var narration = await repository.HeadAsync(narrationId, ct);
        if (narration is null)
            return Op.NotFound("Không tìm thấy thuyết minh.");
        if (narration.Status == ContentStatus.Inactive)
            return Op.Conflict("Phiên bản thuyết minh đã ngừng công bố.");
        if (!await repository.LanguageExistsAsync(form.LanguageCode, ct))
            return Op.Invalid("Ngôn ngữ chưa được hỗ trợ.");

        var before = await repository.GetAsync(narrationId, ct);
        var existing = await repository.TranslationOfAsync(narrationId, form.LanguageCode, ct);
        if (existing is null)
        {
            if (!await repository.AddTranslationAsync(narrationId, form.LanguageCode, form.Content, ct))
                return Op.Conflict("Bản dịch ngôn ngữ này vừa được thêm bởi người khác. Hãy tải lại và sửa.");
        }
        else
        {
            if (existing.Status == ContentStatus.Published)
                return Op.Conflict("Bản dịch đang công bố. Hãy ngừng công bố trước khi sửa.");
            await repository.UpdateTranslationAsync(existing.Id, form.Content, ct);
        }
        return await FinishAsync(narrationId, actor, existing is null ? "Thêm bản dịch" : "Sửa bản dịch", before, null, ct);
    }

    public async Task<OpResult<NarrationInfo>> SetTranslationStatusAsync(int id, string status, bool canPublish, int actor, CancellationToken ct)
    {
        var translation = await repository.TranslationAsync(id, ct);
        if (translation is null)
            return Op.NotFound("Không tìm thấy bản dịch.");
        var ok = (translation.Status, status) is (ContentStatus.Draft, ContentStatus.Pending) or (ContentStatus.Pending, ContentStatus.Approved)
            or (ContentStatus.Pending, ContentStatus.Draft) or (ContentStatus.Approved, ContentStatus.Draft) or (ContentStatus.Published, ContentStatus.Inactive);
        if (!ok)
            return Op.Invalid($"Không thể chuyển bản dịch từ \"{ContentStatus.Label(translation.Status)}\" sang \"{ContentStatus.Label(status)}\".");
        if (status is ContentStatus.Approved or ContentStatus.Inactive && !canPublish)
            return Op.Forbidden(NoPermission);
        var before = await repository.GetAsync(translation.NarrationId, ct);
        await repository.SetTranslationStatusAsync(id, status, ct);
        return await FinishAsync(translation.NarrationId, actor, $"Bản dịch → {ContentStatus.Label(status)}", before, null, ct);
    }

    /// <summary>Lưu kết quả TTS do ứng dụng quản trị tạo ra (tệp WAV) hoặc ghi nhận lỗi (trạng thái Failed) để xử lý lại.</summary>
    public async Task<OpResult<NarrationInfo>> SaveAudioAsync(int translationId, AudioUpload upload, int actor, CancellationToken ct)
    {
        var translation = await repository.TranslationAsync(translationId, ct);
        if (translation is null)
            return Op.NotFound("Không tìm thấy bản dịch.");
        if (translation.Status == ContentStatus.Inactive)
            return Op.Conflict("Bản dịch đã ngừng công bố.");
        var voice = upload.Voice.Trim();
        if (voice.Length == 0)
            return Op.Invalid("Vui lòng chọn giọng đọc.");

        var fileName = "";
        var status = ContentStatus.Failed;
        if (string.IsNullOrWhiteSpace(upload.Error) && !string.IsNullOrWhiteSpace(upload.WavBase64))
        {
            byte[] bytes;
            try { bytes = Convert.FromBase64String(upload.WavBase64); }
            catch (FormatException) { return Op.Invalid("Tệp audio không đúng định dạng."); }
            if (bytes.Length == 0 || bytes.Length > MaxAudioBytes)
                return Op.Invalid("Tệp audio rỗng hoặc vượt quá 20 MB.");
            if (!IsWave(bytes))
                return Op.Invalid("Chỉ nhận tệp WAV.");
            fileName = await files.SaveAsync(translationId, bytes, ct);
            status = ContentStatus.Pending;
        }

        int audioId;
        try
        {
            audioId = await repository.AddAudioAsync(translationId, voice, fileName, status, ct);
        }
        catch
        {
            if (fileName.Length > 0) files.Delete(fileName);
            throw;
        }
        await administration.RecordAsync(new AuditEntry(actor, status == ContentStatus.Failed ? "Tạo audio lỗi" : "Tạo audio", "TEPAUDIO", audioId,
            status == ContentStatus.Failed ? Truncate(upload.Error, 1000) : null, null, new { audioId, translationId, voice, status, fileName }), ct);
        return Reload(await repository.GetAsync(translation.NarrationId, ct));
    }

    public async Task<OpResult<NarrationInfo>> SetAudioStatusAsync(int id, string status, bool canPublish, int actor, CancellationToken ct)
    {
        var audio = await repository.AudioAsync(id, ct);
        if (audio is null)
            return Op.NotFound("Không tìm thấy tệp audio.");
        if (status is not (ContentStatus.Published or ContentStatus.Inactive) || audio.Status == status)
            return Op.Invalid("Trạng thái audio không hợp lệ.");
        if (!canPublish)
            return Op.Forbidden(NoPermission);
        if (status == ContentStatus.Published)
        {
            if (audio.Status != ContentStatus.Pending)
                return Op.Conflict("Chỉ công bố được audio ở trạng thái chờ.");
            if (audio.TranslationStatus is not (ContentStatus.Approved or ContentStatus.Published))
                return Op.Conflict("Bản dịch cần được duyệt trước khi công bố audio.");
            if (audio.NarrationStatus is not (ContentStatus.Approved or ContentStatus.Published))
                return Op.Conflict("Thuyết minh cần được duyệt trước khi công bố audio.");
            if (files.Find(audio.FileName) is null)
                return Op.Conflict("Không tìm thấy tệp audio trên máy chủ. Hãy tạo lại bằng TTS.");
            await repository.PublishAudioAsync(id, ct);
        }
        else
            await repository.UnpublishAudioAsync(id, ct);
        await administration.RecordAsync(new AuditEntry(actor, status == ContentStatus.Published ? "Công bố audio" : "Ngừng công bố audio", "TEPAUDIO", id, null, audio, new { audio.Id, Status = status }), ct);
        return Reload(await repository.GetAsync(audio.NarrationId, ct));
    }

    /// <summary>Đường dẫn tệp audio cho trình nghe thử của biên tập viên.</summary>
    public async Task<OpResult<string>> AudioPathAsync(int id, CancellationToken ct)
    {
        var path = files.Find(await repository.AudioFileNameAsync(id, ct));
        return path is null ? Op.NotFound("Không tìm thấy tệp audio.") : Op.Ok(path);
    }

    private async Task<OpResult<NarrationInfo>> FinishAsync(int narrationId, int actor, string action, NarrationInfo? before, string? reason, CancellationToken ct)
    {
        var after = await repository.GetAsync(narrationId, ct);
        await administration.RecordAsync(new AuditEntry(actor, action, "THUYETMINH", narrationId, reason, before, after is null ? null : new { after.Id, after.DestinationId, after.Title, after.Version, after.Status }), ct);
        return Reload(after);
    }

    private static OpResult<NarrationInfo> Reload(NarrationInfo? narration) => narration is null ? Op.NotFound("Không tìm thấy thuyết minh.") : Op.Ok(narration);

    private static bool IsWave(byte[] bytes) =>
        bytes.Length >= 12 && bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F'
        && bytes[8] == 'W' && bytes[9] == 'A' && bytes[10] == 'V' && bytes[11] == 'E';

    private static string? Truncate(string? value, int length) => value is null ? null : value.Length <= length ? value : value[..length];
}
