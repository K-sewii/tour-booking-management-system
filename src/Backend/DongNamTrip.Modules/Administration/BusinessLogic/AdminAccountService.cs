using DongNamTrip.Contracts;
using DongNamTrip.Modules.Booking;

namespace DongNamTrip.Modules.Administration;

public sealed class AdminAccountService(AdminAccountRepository repository, SqlSessionStore sessions, IBookingStatisticsApi statistics)
{
    public Task<List<string>> RolesAsync(CancellationToken ct) => repository.RolesAsync(ct);
    public Task<List<AdminAccount>> AccountsAsync(string? query, string? role, CancellationToken ct) => repository.AccountsAsync(query, role, ct);
    public Task<AdminAccountPage> AccountPageAsync(string group, string? query, string? role, string? status, int page, CancellationToken ct) => repository.AccountPageAsync(group, query, role, status, page, ct);
    public Task<AdminAuditPage> AuditAsync(int page, string? actorType, DateOnly? from, DateOnly? to, string? query, CancellationToken ct) => repository.AuditAsync(page, actorType, from, to, query, ct);
    public Task<AdminStatistics> StatisticsAsync(DateOnly from, DateOnly to, string? status, CancellationToken ct) => statistics.StatisticsAsync(from, to, status, ct);
    public Task<AdminMutation> RolesAsync(int id, int actor, AdminRoleUpdate input, CancellationToken ct) => input.Roles is null || input.ExpectedRoles is null || input.Roles.Any(string.IsNullOrWhiteSpace) || input.ExpectedRoles.Any(string.IsNullOrWhiteSpace) ? Task.FromResult(AdminMutation.Invalid) : repository.RolesAsync(id, actor, input, ct);
    public async Task<AdminMutation> StatusAsync(int id, int actor, AdminStatusUpdate input, CancellationToken ct)
    {
        var result = await repository.StatusAsync(id, actor, input, ct);
        if (result == AdminMutation.Done && input.Status == "Locked") sessions.RemoveAllForAccount(id);
        return result;
    }
}
