using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DongNamTrip.Contracts;

namespace DongNamTrip.Client;

// Phần dùng cho địa điểm, hoạt động, suất, thuyết minh, audio và quyền nghe (Vương).
public partial class TripApiClient
{
    // ---- Khách (SCR06, SCR07, SCR11) ----
    public Task<DestinationDetail> DestinationDetailAsync(int id, CancellationToken ct = default) =>
        GetAsync<DestinationDetail>($"api/destinations/{id}", ct: ct);
    public Task<ListenCatalog> ListenCatalogAsync(string token, CancellationToken ct = default) =>
        GetAsync<ListenCatalog>("api/listen", token, ct);

    // ---- SCR14: địa điểm ----
    public Task<List<AdminDestination>> AdminDestinationsAsync(string token, CancellationToken ct = default) =>
        GetAsync<List<AdminDestination>>("api/admin/destinations", token, ct);
    public Task<AdminDestination> SaveAdminDestinationAsync(int? id, DestinationForm data, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminDestination>(id is null ? HttpMethod.Post : HttpMethod.Put,
            "api/admin/destinations" + (id is null ? "" : $"/{id}"), data, token, ct);
    public Task<AdminDestination> SetDestinationStatusAsync(int id, string status, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminDestination>(HttpMethod.Post, $"api/admin/destinations/{id}/status", new StatusChange(status), token, ct);
    public async Task<DeleteResult> DeleteDestinationAsync(int id, string token, CancellationToken ct = default)
    {
        using var request = Request(HttpMethod.Delete, $"api/admin/destinations/{id}", token);
        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.OK)
            return (await response.Content.ReadFromJsonAsync<DeleteResult>(cancellationToken: ct))!;
        await ThrowWithMessageAsync(response);
        return new DeleteResult(false, "");
    }

    // ---- SCR15: hoạt động và suất ----
    public Task<List<DestinationOption>> DestinationOptionsAsync(string token, CancellationToken ct = default) =>
        GetAsync<List<DestinationOption>>("api/admin/destination-options", token, ct);
    public Task<List<AdminActivity>> AdminActivitiesAsync(int? destinationId, string token, CancellationToken ct = default) =>
        GetAsync<List<AdminActivity>>("api/admin/activities" + (destinationId is null ? "" : $"?destinationId={destinationId}"), token, ct);
    public Task<AdminActivity> SaveActivityAsync(int? id, ActivityForm data, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminActivity>(id is null ? HttpMethod.Post : HttpMethod.Put,
            "api/admin/activities" + (id is null ? "" : $"/{id}"), data, token, ct);
    public Task<ImpactResult> SetActivityStatusAsync(int id, string status, bool confirm, string token, CancellationToken ct = default) =>
        SendImpactAsync($"api/admin/activities/{id}/status?confirm={confirm.ToString().ToLowerInvariant()}", new StatusChange(status), token, ct);
    public Task<SlotInfo> SaveSlotAsync(int activityId, int? slotId, SlotForm data, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<SlotInfo>(slotId is null ? HttpMethod.Post : HttpMethod.Put,
            slotId is null ? $"api/admin/activities/{activityId}/slots" : $"api/admin/slots/{slotId}", data, token, ct);
    public Task<ImpactResult> SetSlotStatusAsync(int id, string status, bool confirm, string token, CancellationToken ct = default) =>
        SendImpactAsync($"api/admin/slots/{id}/status?confirm={confirm.ToString().ToLowerInvariant()}", new StatusChange(status), token, ct);

    // ---- SCR20: thuyết minh, bản dịch, audio ----
    public Task<List<LanguageInfo>> LanguagesAsync(string token, CancellationToken ct = default) =>
        GetAsync<List<LanguageInfo>>("api/admin/languages", token, ct);
    public Task<List<NarrationInfo>> NarrationsAsync(string token, CancellationToken ct = default) =>
        GetAsync<List<NarrationInfo>>("api/admin/narrations", token, ct);
    public Task<NarrationInfo> SaveNarrationAsync(int? id, NarrationForm data, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<NarrationInfo>(id is null ? HttpMethod.Post : HttpMethod.Put,
            "api/admin/narrations" + (id is null ? "" : $"/{id}"), data, token, ct);
    public Task<NarrationInfo> NewNarrationVersionAsync(int id, NarrationForm data, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<NarrationInfo>(HttpMethod.Post, $"api/admin/narrations/{id}/versions", data, token, ct);
    public Task<NarrationInfo> SetNarrationStatusAsync(int id, string status, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<NarrationInfo>(HttpMethod.Post, $"api/admin/narrations/{id}/status", new StatusChange(status), token, ct);
    public Task<NarrationInfo> SaveTranslationAsync(int narrationId, TranslationForm data, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<NarrationInfo>(HttpMethod.Put, $"api/admin/narrations/{narrationId}/translations", data, token, ct);
    public Task<NarrationInfo> SetTranslationStatusAsync(int id, string status, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<NarrationInfo>(HttpMethod.Post, $"api/admin/translations/{id}/status", new StatusChange(status), token, ct);
    public Task<NarrationInfo> UploadAudioAsync(int translationId, AudioUpload data, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<NarrationInfo>(HttpMethod.Post, $"api/admin/translations/{translationId}/audio", data, token, ct);
    public Task<NarrationInfo> SetAudioStatusAsync(int id, string status, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<NarrationInfo>(HttpMethod.Post, $"api/admin/audio/{id}/status", new StatusChange(status), token, ct);
    public async Task<byte[]> AudioBytesAsync(int id, string token, CancellationToken ct = default)
    {
        using var request = Request(HttpMethod.Get, $"api/admin/audio/{id}/file", token);
        using var response = await http.SendAsync(request, ct);
        await CheckAsync(response);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    // ---- Quyền nghe: Đức gọi khi xác nhận/hủy đơn ----
    public Task<ListenRightGrant> GrantListenRightAsync(ListenRightGrant data, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<ListenRightGrant>(HttpMethod.Post, "api/internal/listen-rights", data, token, ct);
    public async Task RevokeListenRightAsync(int orderId, string token, CancellationToken ct = default)
    {
        using var request = Request(HttpMethod.Delete, $"api/internal/listen-rights/{orderId}", token);
        using var response = await http.SendAsync(request, ct);
        await CheckAsync(response);
    }

    // ---- Hỗ trợ: giữ nguyên thông báo lỗi nghiệp vụ do API trả về ----
    private async Task<T> SendWithMessageAsync<T>(HttpMethod method, string path, object data, string token, CancellationToken ct)
    {
        using var request = Request(method, path, token);
        request.Content = JsonContent.Create(data);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) await ThrowWithMessageAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    /// <summary>Đổi trạng thái có thể ảnh hưởng lịch trình: 200 (Done) hoặc 409 kèm ImpactResult (NeedConfirm). Mọi lỗi khác đưa ra kèm thông báo của API.</summary>
    private async Task<ImpactResult> SendImpactAsync(string path, object data, string token, CancellationToken ct)
    {
        using var request = Request(HttpMethod.Post, path, token);
        request.Content = JsonContent.Create(data);
        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (TryReadImpact(body) is { } impact) return impact;
            throw new HttpRequestException(MessageOf(body) ?? "Không thực hiện được thao tác.", null, response.StatusCode);
        }
        await ThrowWithMessageAsync(response);
        return new ImpactResult(ImpactStatuses.Done, 0, "");
    }

    private static ImpactResult? TryReadImpact(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            // ProblemDetails cũng có "status" nhưng là số; ImpactResult.status là chuỗi.
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String)
                return JsonSerializer.Deserialize<ImpactResult>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException) { }
        return null;
    }

    private static async Task ThrowWithMessageAsync(HttpResponseMessage response)
    {
        var message = MessageOf(await response.Content.ReadAsStringAsync());
        if (string.IsNullOrWhiteSpace(message))
            await CheckAsync(response);
        throw new HttpRequestException(message, null, response.StatusCode);
    }

    /// <summary>Lời giải thích nghiệp vụ trong ProblemDetails (<c>detail</c>) hoặc lỗi theo trường (<c>errors</c>); null nếu không có.</summary>
    private static string? MessageOf(string body)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String) return detail.GetString();
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String) return message.GetString();
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                return string.Join(" ", errors.EnumerateObject().SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString())));
        }
        catch (JsonException) { }
        return null;
    }
}
