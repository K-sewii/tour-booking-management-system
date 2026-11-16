namespace DongNamTrip.Contracts;

// Administration: người dùng, nhật ký, thống kê (UC24, SCR13, SCR19).
public record UserFilter(string? Q, string? Role, string? Status, int? Page, int? Size);
public record AdminUser(int Id, string Email, string FullName, string? Phone, string Status, IReadOnlyList<string> Roles, DateTime CreatedAt);
public record AdminUserDetail(AdminUser User, int ActiveSessions, IReadOnlyList<AuditRow> RecentActions);
public record StaffForm(string Email, string FullName, string? Phone, IReadOnlyList<string> Roles);
public record StaffCreated(AdminUser User, string TemporaryPassword);
public record RolesRequest(IReadOnlyList<string> Roles, IReadOnlyList<string>? ExpectedRoles = null);
public record TempPassword(int AccountId, string Password);
public record AuditFilter(DateTime? From, DateTime? To, string? ObjectType, int? ActorId, int? Page, int? Size);
public record AuditRow(long Id, int? ActorId, string ActorName, DateTime At, string Action, string ObjectType, string ObjectId, string? Reason, string? Before, string? After);
public record OperationSummary(DateTime From, DateTime To, IReadOnlyList<StatusCount> Orders, decimal OrderValue, decimal Received, decimal Refunded, IReadOnlyList<StatusCount> Requests);
public record StatusCount(string Status, int Count, decimal OrderValue = 0);
