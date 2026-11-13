using DongNamTrip.Contracts;
using DongNamTrip.Modules.Booking;

namespace DongNamTrip.Modules.Administration;

public sealed class AdminAccountService(AdminAccountRepository repository, UserAdminService users, IBookingStatisticsApi statistics)
{
    public Task<List<string>> RolesAsync(CancellationToken ct) => repository.RolesAsync(ct);
    public Task<List<AdminAccount>> AccountsAsync(string? query, string? role, CancellationToken ct) => repository.AccountsAsync(query, role, ct);
    public Task<AdminAccountPage> AccountPageAsync(string group, string? query, string? role, string? status, int page, CancellationToken ct) => repository.AccountPageAsync(group, query, role, status, page, ct);
    public Task<AdminAuditPage> AuditAsync(int page, string? actorType, DateOnly? from, DateOnly? to, string? query, CancellationToken ct) => repository.AuditAsync(page, actorType, from, to, query, ct);
    public Task<AdminStatistics> StatisticsAsync(DateOnly from, DateOnly to, string? status, CancellationToken ct) => statistics.StatisticsAsync(from, to, status, ct);
    public async Task<AdminMutation> RolesAsync(int id, int actor, AdminRoleUpdate input, CancellationToken ct)
    {
        if (input.Roles is null || input.ExpectedRoles is null || input.Roles.Any(string.IsNullOrWhiteSpace) || input.ExpectedRoles.Any(string.IsNullOrWhiteSpace)) return AdminMutation.Invalid;
        return ToMutation(await users.SetRolesAsync(id, input.Roles, actor, ct, input.ExpectedRoles));
    }
    public async Task<AdminMutation> StatusAsync(int id, int actor, AdminStatusUpdate input, CancellationToken ct)
    {
        if (input.ExpectedStatus is null) return AdminMutation.Invalid;
        return ToMutation(await users.SetStatusAsync(id, input.Status, null, actor, ct, input.ExpectedStatus));
    }

    private static AdminMutation ToMutation(OpResult<AdminUser> result) => result.Status switch
    {
        OpStatus.Ok => AdminMutation.Done,
        OpStatus.NotFound => AdminMutation.NotFound,
        OpStatus.Conflict => AdminMutation.Conflict,
        _ => AdminMutation.Invalid
    };
}
