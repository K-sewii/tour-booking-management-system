using System.Net.Http.Json;
using DongNamTrip.Contracts;

namespace DongNamTrip.Client;

// Tour, đơn đặt, tiền, yêu cầu tour riêng, báo giá, người dùng, nhật ký, thống kê (SCR13, SCR16-SCR19) và các route khách tương ứng.
public partial class TripApiClient
{
    private async Task<T> GetWithMessageAsync<T>(string path, string token, CancellationToken ct)
    {
        using var request = Request(HttpMethod.Get, path, token);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) await ThrowWithMessageAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    private static string Query(params (string Key, object? Value)[] pairs) =>
        string.Join("&", pairs.Where(pair => pair.Value is not null).Select(pair => $"{pair.Key}={Uri.EscapeDataString(Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture) ?? "")}")) is { Length: > 0 } text ? "?" + text : "";

    // ---- Khách: lịch khởi hành, đặt tour, hủy đơn (SCR05, SCR10) ----
    public Task<List<DepartureInfo>> TourDeparturesAsync(int tourId, CancellationToken ct = default) => GetAsync<List<DepartureInfo>>($"api/tours/{tourId}/departures", ct: ct);
    public Task<BookingInfo> PlaceBookingAsync(BookingForm form, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<BookingInfo>(HttpMethod.Post, "api/account/bookings", form, token, ct);
    public Task<List<BookingInfo>> BookingsAsync(string token, CancellationToken ct = default) => GetAsync<List<BookingInfo>>("api/account/bookings", token, ct);
    public Task<BookingDetail> BookingAsync(int id, string token, CancellationToken ct = default) => GetWithMessageAsync<BookingDetail>($"api/account/bookings/{id}", token, ct);
    public Task<BookingInfo> CancelBookingAsync(int id, string reason, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<BookingInfo>(HttpMethod.Post, $"api/account/bookings/{id}/cancel", new CancelRequest(reason), token, ct);

    // ---- Khách: yêu cầu tour riêng và báo giá (SCR10) ----
    public Task<QuoteRequestInfo> SubmitQuoteRequestAsync(QuoteRequestForm form, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<QuoteRequestInfo>(HttpMethod.Post, "api/account/quote-requests", form, token, ct);
    public Task<List<QuoteRequestInfo>> QuoteRequestsAsync(string token, CancellationToken ct = default) => GetAsync<List<QuoteRequestInfo>>("api/account/quote-requests", token, ct);
    public Task<QuoteRequestDetail> QuoteRequestAsync(int id, string token, CancellationToken ct = default) => GetWithMessageAsync<QuoteRequestDetail>($"api/account/quote-requests/{id}", token, ct);
    public Task<QuoteRequestInfo> CancelQuoteRequestAsync(int id, string reason, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<QuoteRequestInfo>(HttpMethod.Post, $"api/account/quote-requests/{id}/cancel", new CancelRequest(reason), token, ct);
    public Task<QuoteAccepted> AcceptQuoteAsync(int quoteId, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<QuoteAccepted>(HttpMethod.Post, $"api/account/quotes/{quoteId}/accept", new { }, token, ct);
    public Task<QuoteRequestInfo> AskQuoteChangeAsync(int quoteId, string note, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<QuoteRequestInfo>(HttpMethod.Post, $"api/account/quotes/{quoteId}/change-request", new ChangeRequest(note), token, ct);

    // ---- Lịch trình: kiểm tra, xóa (SCR08) ----
    public Task<ItineraryCheck> CheckDraftAsync(DraftCheckRequest request, CancellationToken ct = default) =>
        SendAsync<ItineraryCheck>(HttpMethod.Post, "api/itineraries/check", request, null, ct);
    public Task<ItineraryCheck> CheckItineraryAsync(int id, string token, CancellationToken ct = default) => GetWithMessageAsync<ItineraryCheck>($"api/itineraries/{id}/check", token, ct);
    public Task DeleteItineraryAsync(int id, string token, CancellationToken ct = default) => NoContentAsync(HttpMethod.Delete, $"api/itineraries/{id}", token, ct: ct);

    // ---- SCR16: tour và lịch khởi hành ----
    public Task<List<AdminTour>> AdminToursAsync(string? status, string token, CancellationToken ct = default) => GetWithMessageAsync<List<AdminTour>>("api/admin/tours" + Query(("status", status)), token, ct);
    public Task<AdminTourDetail> AdminTourAsync(int id, string token, CancellationToken ct = default) => GetWithMessageAsync<AdminTourDetail>($"api/admin/tours/{id}", token, ct);
    public Task<AdminTour> SaveAdminTourAsync(int? id, TourForm form, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminTour>(id is null ? HttpMethod.Post : HttpMethod.Put, "api/admin/tours" + (id is null ? "" : $"/{id}"), form, token, ct);
    public Task<AdminTourDetail> SetTourPlacesAsync(int id, IReadOnlyList<TourStopForm> stops, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminTourDetail>(HttpMethod.Put, $"api/admin/tours/{id}/places", stops, token, ct);
    public Task<AdminTour> SetTourStatusAsync(int id, string status, string? reason, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminTour>(HttpMethod.Post, $"api/admin/tours/{id}/status", new StatusRequest(status, reason), token, ct);
    public Task<AdminDeparture> CreateDepartureAsync(int tourId, DepartureForm form, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminDeparture>(HttpMethod.Post, $"api/admin/tours/{tourId}/departures", form, token, ct);
    public Task<AdminDeparture> UpdateDepartureAsync(int id, DepartureForm form, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminDeparture>(HttpMethod.Put, $"api/admin/departures/{id}", form, token, ct);
    public Task<AdminDeparture> DeactivateDepartureAsync(int id, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminDeparture>(HttpMethod.Post, $"api/admin/departures/{id}/deactivate", new { }, token, ct);

    // ---- SCR17: đơn đặt tour, thu hoàn ----
    public Task<PagedResult<AdminBooking>> AdminBookingsAsync(BookingFilter filter, string token, CancellationToken ct = default) =>
        GetWithMessageAsync<PagedResult<AdminBooking>>("api/admin/bookings" + Query(("status", filter.Status), ("from", filter.From?.ToString("s")), ("to", filter.To?.ToString("s")),
            ("q", filter.Q), ("overdue", filter.Overdue is true ? "true" : null), ("page", filter.Page), ("size", filter.Size)), token, ct);
    public Task<AdminBookingDetail> AdminBookingAsync(int id, string token, CancellationToken ct = default) => GetWithMessageAsync<AdminBookingDetail>($"api/admin/bookings/{id}", token, ct);
    public Task<AdminBooking> ConfirmBookingAsync(int id, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminBooking>(HttpMethod.Post, $"api/admin/bookings/{id}/confirm", new { }, token, ct);
    public Task<AdminBookingDetail> AdjustBookingAsync(int id, AdjustForm form, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminBookingDetail>(HttpMethod.Post, $"api/admin/bookings/{id}/adjust", form, token, ct);
    public Task<AdminBooking> DecideCancelAsync(int id, CancelDecision decision, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminBooking>(HttpMethod.Post, $"api/admin/bookings/{id}/cancel-decision", decision, token, ct);
    public Task<MoneyEntry> RecordMoneyAsync(int id, MoneyForm form, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<MoneyEntry>(HttpMethod.Post, $"api/admin/bookings/{id}/money", form, token, ct);
    public Task<AdminBooking> CompleteBookingAsync(int id, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminBooking>(HttpMethod.Post, $"api/admin/bookings/{id}/complete", new { }, token, ct);

    // ---- SCR18: yêu cầu tour riêng và báo giá (nhân viên) ----
    public Task<PagedResult<QuoteRequestInfo>> AdminQuoteRequestsAsync(QuoteRequestFilter filter, string token, CancellationToken ct = default) =>
        GetWithMessageAsync<PagedResult<QuoteRequestInfo>>("api/admin/quote-requests" + Query(("status", filter.Status), ("from", filter.From?.ToString("s")), ("to", filter.To?.ToString("s")),
            ("page", filter.Page), ("size", filter.Size)), token, ct);
    public Task<AdminQuoteRequestDetail> AdminQuoteRequestAsync(int id, string token, CancellationToken ct = default) =>
        GetWithMessageAsync<AdminQuoteRequestDetail>($"api/admin/quote-requests/{id}", token, ct);
    public Task<QuoteRequestInfo> ReceiveQuoteRequestAsync(int id, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<QuoteRequestInfo>(HttpMethod.Post, $"api/admin/quote-requests/{id}/receive", new { }, token, ct);
    public Task<VerificationInfo> RecordVerificationAsync(int id, VerificationForm form, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<VerificationInfo>(HttpMethod.Post, $"api/admin/quote-requests/{id}/verifications", form, token, ct);
    public Task<QuoteInfo> IssueQuoteAsync(int id, QuoteForm form, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<QuoteInfo>(HttpMethod.Post, $"api/admin/quote-requests/{id}/quotes", form, token, ct);
    public Task<QuoteRequestInfo> CancelQuoteRequestByStaffAsync(int id, string reason, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<QuoteRequestInfo>(HttpMethod.Post, $"api/admin/quote-requests/{id}/cancel", new CancelRequest(reason), token, ct);

    // ---- SCR19, SCR13: người dùng, nhật ký, thống kê ----
    public Task<PagedResult<AdminUser>> AdminUsersAsync(UserFilter filter, string token, CancellationToken ct = default) =>
        GetWithMessageAsync<PagedResult<AdminUser>>("api/admin/users" + Query(("q", filter.Q), ("role", filter.Role), ("status", filter.Status), ("page", filter.Page), ("size", filter.Size)), token, ct);
    public Task<AdminUserDetail> AdminUserAsync(int id, string token, CancellationToken ct = default) => GetWithMessageAsync<AdminUserDetail>($"api/admin/users/{id}", token, ct);
    public Task<StaffCreated> CreateStaffAsync(StaffForm form, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<StaffCreated>(HttpMethod.Post, "api/admin/users", form, token, ct);
    public Task<AdminUser> SetUserRolesAsync(int id, IReadOnlyList<string> roles, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminUser>(HttpMethod.Put, $"api/admin/users/{id}/roles", new RolesRequest(roles), token, ct);
    public Task<AdminUser> SetUserStatusAsync(int id, string status, string? reason, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<AdminUser>(HttpMethod.Post, $"api/admin/users/{id}/status", new StatusRequest(status, reason), token, ct);
    public Task<TempPassword> ResetUserPasswordAsync(int id, string token, CancellationToken ct = default) =>
        SendWithMessageAsync<TempPassword>(HttpMethod.Post, $"api/admin/users/{id}/reset-password", new { }, token, ct);
    public Task<PagedResult<AuditRow>> AuditAsync(AuditFilter filter, string token, CancellationToken ct = default) =>
        GetWithMessageAsync<PagedResult<AuditRow>>("api/admin/audit" + Query(("from", filter.From?.ToString("s")), ("to", filter.To?.ToString("s")), ("objectType", filter.ObjectType),
            ("actorId", filter.ActorId), ("page", filter.Page), ("size", filter.Size)), token, ct);
    public Task<OperationSummary> StatsAsync(DateTime from, DateTime to, string? status, string token, CancellationToken ct = default) =>
        GetWithMessageAsync<OperationSummary>("api/admin/stats" + Query(("from", from.ToString("s")), ("to", to.ToString("s")), ("status", status)), token, ct);
}
