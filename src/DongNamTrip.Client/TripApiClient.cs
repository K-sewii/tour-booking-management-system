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
    public async Task LogoutAsync(string token)
    {
        using var request = Request(HttpMethod.Post, "api/auth/logout", token);
        using var response = await http.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            await CheckAsync(response);
    }

    public Task<Destination> SaveDestinationAsync(int? id, DestinationDraft data, string token) => SendAsync<Destination>(id is null ? HttpMethod.Post : HttpMethod.Put, "api/destinations" + (id is null ? "" : $"/{id}"), data, token);
    public Task<Itinerary> SaveItineraryAsync(ItineraryDraft data, string token) => SendAsync<Itinerary>(HttpMethod.Post, "api/itineraries", data, token);
    private static HttpRequestMessage Request(HttpMethod method, string path, string? token)
    {
        var req = new HttpRequestMessage(method, path);
        if (token is not null)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
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

    private static async Task CheckAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;
        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Thông tin đăng nhập sai hoặc phiên đã hết hạn. Vui lòng đăng nhập lại.",
            HttpStatusCode.Forbidden => "Tài khoản không có quyền thực hiện thao tác này.",
            HttpStatusCode.BadRequest => "Dữ liệu chưa hợp lệ. Kiểm tra thông tin, ngày đi và điểm đã chọn.",
            HttpStatusCode.NotFound => "Không tìm thấy dữ liệu yêu cầu.",
            _ => "API chưa xử lý được yêu cầu. Vui lòng thử lại."
        };
        await Task.CompletedTask;
        throw new HttpRequestException(message, null, response.StatusCode);
    }
}
