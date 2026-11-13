using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Administration;

public sealed class AdminAccountService(AuditService audit, UserAdminService users, StatisticsService statistics)
{
    public Task<List<string>> RolesAsync(CancellationToken ct) => users.RolesAsync(ct);
    public Task<List<AdminAccount>> AccountsAsync(string? query, string? role, CancellationToken ct) => users.AccountsAsync(query, role, ct);
    public Task<OpResult<AdminAccountPage>> AccountPageAsync(string? group, string? query, string? role, string? status, int? page, CancellationToken ct) => users.GroupedAsync(group, query, role, status, page, ct);
    public Task<OpResult<AdminAuditPage>> AuditAsync(int? page, string? actorType, DateOnly? from, DateOnly? to, string? query, CancellationToken ct) => audit.QueryScreenAsync(page, actorType, from, to, query, ct);
    public async Task<OpResult<AdminStatistics>> StatisticsAsync(DateOnly from, DateOnly to, string? status, CancellationToken ct)
    {
        var summary = await statistics.SummaryAsync(from.ToDateTime(TimeOnly.MinValue), to.ToDateTime(TimeOnly.MinValue), status, ct);
        if (!summary.IsOk) return new OpFailure(summary.Status, summary.Message ?? "Invalid statistics filter.");
        var result = summary.Value!;
        return Op.Ok(new AdminStatistics(from, to, result.OrderValue, result.Received, result.Refunded, null,
            result.Orders.Select(x => new AdminStatisticsRow(x.Status, x.Count, x.OrderValue)).ToList(), OrderStatus.All.ToList()));
    }
    public async Task<AdminMutation> RolesAsync(int id, int actor, AdminRoleUpdate input, CancellationToken ct)
    {
        if (input.Roles is null || input.ExpectedRoles is null || input.Roles.Any(string.IsNullOrWhiteSpace) || input.ExpectedRoles.Any(string.IsNullOrWhiteSpace)) return AdminMutation.Invalid;
        return ToMutation(await users.SetRolesAsync(id, input.Roles, actor, ct, input.ExpectedRoles));
    }
    public async Task<AdminMutation> StatusAsync(int id, int actor, AdminStatusUpdate input, CancellationToken ct)
    {
        if (input.ExpectedStatus is null) return AdminMutation.Invalid;
        return ToMutation(await users.SetStatusAsync(id, input.Status, input.Reason, actor, ct, input.ExpectedStatus));
    }

    private static AdminMutation ToMutation(OpResult<AdminUser> result) => result.Status switch
    {
        OpStatus.Ok => AdminMutation.Done,
        OpStatus.NotFound => AdminMutation.NotFound,
        OpStatus.Conflict => AdminMutation.Conflict,
        _ => AdminMutation.Invalid
    };
}
