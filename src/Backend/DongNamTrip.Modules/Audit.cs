namespace DongNamTrip.Modules;

/// <summary>Một dòng nhật ký thao tác (UC24, BR13). Module nào cũng ghi qua <c>IAdministrationApi</c>.</summary>
public sealed record AuditEntry(int ActorId, string Action, string ObjectType, object ObjectId, string? Reason = null, object? Before = null, object? After = null);
