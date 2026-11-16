using DongNamTrip.Contracts;

namespace DongNamTrip.Client;

public partial class TripApiClient
{
    public Task<AdminAuditPage> AdminAuditAsync(int page, string actorType, DateOnly? from, DateOnly? to, string query, string token, CancellationToken ct = default) => GetAsync<AdminAuditPage>($"api/admin/audit/search?page={page}&actorType={Uri.EscapeDataString(actorType)}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&query={Uri.EscapeDataString(query)}", token, ct);
}
