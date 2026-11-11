using DongNamTrip.Contracts;

namespace DongNamTrip.Client;

public partial class TripApiClient
{
    public Task<List<AdminAccount>> AdminAccountsAsync(string query, string role, string token, CancellationToken ct = default) => GetAsync<List<AdminAccount>>($"api/admin/accounts?query={Uri.EscapeDataString(query)}&role={Uri.EscapeDataString(role)}", token, ct);
    public Task<AdminAccountPage> AdminAccountPageAsync(string group, string query, string role, string status, int page, string token, CancellationToken ct = default) => GetAsync<AdminAccountPage>($"api/admin/accounts/paged?group={Uri.EscapeDataString(group)}&query={Uri.EscapeDataString(query)}&role={Uri.EscapeDataString(role)}&status={Uri.EscapeDataString(status)}&page={page}", token, ct);
    public Task<List<string>> AdminRolesAsync(string token, CancellationToken ct = default) => GetAsync<List<string>>("api/admin/accounts/roles", token, ct);
    public Task<AdminAuditPage> AdminAuditAsync(int page, string token, CancellationToken ct = default) => GetAsync<AdminAuditPage>($"api/admin/accounts/audit?page={page}", token, ct);
    public Task<AdminAuditPage> AdminAuditAsync(int page, string actorType, string token, CancellationToken ct = default) => GetAsync<AdminAuditPage>($"api/admin/accounts/audit?page={page}&actorType={Uri.EscapeDataString(actorType)}", token, ct);
    public Task<AdminAuditPage> AdminAuditAsync(int page, string actorType, DateOnly? from, DateOnly? to, string query, string token, CancellationToken ct = default) => GetAsync<AdminAuditPage>($"api/admin/accounts/audit?page={page}&actorType={Uri.EscapeDataString(actorType)}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&query={Uri.EscapeDataString(query)}", token, ct);
    public Task<AdminStatistics> AdminStatisticsAsync(DateOnly from, DateOnly to, string status, string token, CancellationToken ct = default) => GetAsync<AdminStatistics>($"api/admin/accounts/statistics?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&status={Uri.EscapeDataString(status)}", token, ct);
    public Task SetAdminRolesAsync(int id, AdminRoleUpdate input, string token, CancellationToken ct = default) => NoContentAsync(HttpMethod.Put, $"api/admin/accounts/{id}/roles", token, input, ct);
    public Task SetAdminStatusAsync(int id, AdminStatusUpdate input, string token, CancellationToken ct = default) => NoContentAsync(HttpMethod.Put, $"api/admin/accounts/{id}/status", token, input, ct);
}
