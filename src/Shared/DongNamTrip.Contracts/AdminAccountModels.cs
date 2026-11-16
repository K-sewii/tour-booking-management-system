namespace DongNamTrip.Contracts;

public sealed record AdminAccount(int Id, string Name, string Email, string Status, string[] Roles, string? Phone = null, DateTime CreatedAt = default);
public sealed record AdminAccountPage(List<AdminAccount> Items, int Total, int Active, int Locked, int Page, int TotalPages, int FilteredTotal = 0);
public sealed record AdminAuditRow(long Id, DateTime Time, string? Actor, string Action, string ObjectType, string? ObjectId, string? Reason, string? Before, string? After, string ActorType = "unknown", string? Result = null);
public sealed record AdminAuditPage(List<AdminAuditRow> Items, bool HasMore, int TotalPages = 1, int Total = 0);
