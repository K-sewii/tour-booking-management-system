using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DongNamTrip.Contracts;

namespace DongNamTrip.Client;

public class TripApiClient(HttpClient http)
{
    public Task<List<Destination>> DestinationsAsync(CancellationToken ct = default) => GetAsync<List<Destination>>("api/destinations", ct: ct);
    public Task<List<Tour>> ToursAsync(CancellationToken ct = default) => GetAsync<List<Tour>>("api/tours", ct: ct);
    public Task<List<Itinerary>> ItinerariesAsync(string token, CancellationToken ct = default) => GetAsync<List<Itinerary>>("api/itineraries", token, ct);
    public Task<LoginResponse> LoginAsync(LoginRequest data, CancellationToken ct = default) => SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", data, null, ct);
    public Task<LoginResponse> RegisterAsync(RegisterRequest data, CancellationToken ct = default) => SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/register", data, null, ct);
    public Task<AuthenticatedUser> MeAsync(string token, CancellationToken ct = default) => GetAsync<AuthenticatedUser>("api/auth/me", token, ct);
    public Task<AccountProfile> ProfileAsync(string token, CancellationToken ct = default) => GetAsync<AccountProfile>("api/auth/profile", token, ct);
    public Task<AccountProfile> UpdateProfileAsync(UpdateProfileRequest data, string token, CancellationToken ct = default) =>
        SendAsync<AccountProfile>(HttpMethod.Put, "api/auth/profile", data, token, ct);
    public Task<List<AccountSession>> SessionsAsync(string token, CancellationToken ct = default) => GetAsync<List<AccountSession>>("api/auth/sessions", token, ct);
    public Task<List<PaymentEntry>> PaymentsAsync(string token, CancellationToken ct = default) => GetAsync<List<PaymentEntry>>("api/account/payments", token, ct);
    public Task<PaymentChoiceDto> PaymentChoiceAsync(string token, CancellationToken ct = default) => GetAsync<PaymentChoiceDto>("api/account/payment-choice", token, ct);
    public Task<PaymentChoiceDto> UpdatePaymentChoiceAsync(PaymentChoiceRequest data, string token, CancellationToken ct = default) => SendAsync<PaymentChoiceDto>(HttpMethod.Put, "api/account/payment-choice", data, token, ct);
    public Task<AccountCollections> CollectionsAsync(string token, CancellationToken ct = default) => GetAsync<AccountCollections>("api/account/collections", token, ct);
    public Task<List<AccountNoticeEntry>> NoticesAsync(string token, CancellationToken ct = default) => GetAsync<List<AccountNoticeEntry>>("api/account/notices", token, ct);
    public Task<AccountPreferenceDto> PreferencesAsync(string token, CancellationToken ct = default) => GetAsync<AccountPreferenceDto>("api/account/preferences", token, ct);
    public Task<AccountPreferenceDto> UpdatePreferencesAsync(AccountPreferenceDto data, string token, CancellationToken ct = default) => SendAsync<AccountPreferenceDto>(HttpMethod.Put, "api/account/preferences", data, token, ct);
    public Task SaveFavoriteAsync(string kind, int id, string token, CancellationToken ct = default) => NoContentAsync(HttpMethod.Put, $"api/account/favorites/{kind}/{id}", token, ct: ct);
    public Task DeleteFavoriteAsync(string kind, int id, string token, CancellationToken ct = default) => NoContentAsync(HttpMethod.Delete, $"api/account/favorites/{kind}/{id}", token, ct: ct);
    public Task RecordActivityAsync(AccountActivityDraft input, string token, CancellationToken ct = default) => NoContentAsync(HttpMethod.Post, "api/account/activity", token, input, ct);
    public Task DeleteActivityAsync(long id, string token, CancellationToken ct = default) => NoContentAsync(HttpMethod.Delete, $"api/account/activity/{id}", token, ct: ct);
    public Task ClearActivityAsync(string kind, string token, CancellationToken ct = default) => NoContentAsync(HttpMethod.Delete, $"api/account/activity?kind={Uri.EscapeDataString(kind)}", token, ct: ct);
    public Task ReadNoticeAsync(long id, string token, CancellationToken ct = default) => NoContentAsync(HttpMethod.Put, $"api/account/notices/{id}/read", token, ct: ct);

    private async Task NoContentAsync(HttpMethod method, string path, string token, object? body = null, CancellationToken ct = default)
    {
        using var request = Request(method, path, token);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await http.SendAsync(request, ct);
        await CheckAsync(response);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest data, string token, CancellationToken ct = default)
    {
        using var request = Request(HttpMethod.Post, "api/auth/password", token);
        request.Content = JsonContent.Create(data);
        using var response = await http.SendAsync(request, ct);
        await CheckAsync(response);
    }
    public async Task RevokeSessionAsync(string id, string token, CancellationToken ct = default)
    {
        using var request = Request(HttpMethod.Delete, "api/auth/sessions/" + Uri.EscapeDataString(id), token);
        using var response = await http.SendAsync(request, ct);
        await CheckAsync(response);
    }

    public async Task LogoutAsync(string token, CancellationToken ct = default)
    {
        using var request = Request(HttpMethod.Post, "api/auth/logout", token);
        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            await CheckAsync(response);
    }

    public Task<Destination> SaveDestinationAsync(int? id, DestinationDraft data, string token, CancellationToken ct = default) =>
        SendAsync<Destination>(id is null ? HttpMethod.Post : HttpMethod.Put, "api/destinations" + (id is null ? "" : $"/{id}"), data, token, ct);

    public Task<Itinerary> SaveItineraryAsync(ItineraryDraft data, string token, CancellationToken ct = default) =>
        SendAsync<Itinerary>(HttpMethod.Post, "api/itineraries", data, token, ct);

    private static HttpRequestMessage Request(HttpMethod method, string path, string? token)
    {
        var request = new HttpRequestMessage(method, path);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<T> GetAsync<T>(string path, string? token = null, CancellationToken ct = default)
    {
        using var request = Request(HttpMethod.Get, path, token);
        using var response = await http.SendAsync(request, ct);
        await CheckAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object data, string? token, CancellationToken ct = default)
    {
        using var request = Request(method, path, token);
        request.Content = JsonContent.Create(data);
        using var response = await http.SendAsync(request, ct);
        await CheckAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    private static Task CheckAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return Task.CompletedTask;

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Email hoặc mật khẩu không đúng.",
            HttpStatusCode.Forbidden => "Tài khoản không có quyền thực hiện thao tác này.",
            HttpStatusCode.TooManyRequests => "Bạn thử quá nhiều lần, vui lòng thử lại sau ít phút.",
            HttpStatusCode.Conflict => "Email đã được đăng ký. Bạn có thể đăng nhập.",
            HttpStatusCode.BadRequest => "Dữ liệu chưa hợp lệ. Vui lòng kiểm tra lại thông tin.",
            HttpStatusCode.NotFound => "Không tìm thấy dữ liệu yêu cầu.",
            _ => "API chưa xử lý được yêu cầu. Vui lòng thử lại."
        };
        throw new HttpRequestException(message, null, response.StatusCode);
    }
}
