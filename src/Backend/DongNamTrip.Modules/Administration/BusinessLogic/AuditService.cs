using System.Text.Json;

namespace DongNamTrip.Modules.Administration;

public sealed class AuditService(AuditRepository repository) : IAdministrationApi
{
    public Task RecordAsync(AuditEntry entry, CancellationToken ct) => repository.AddAsync(
        entry.ActorId, entry.Action, entry.ObjectType, Convert.ToString(entry.ObjectId, System.Globalization.CultureInfo.InvariantCulture) ?? "",
        entry.Reason, Serialize(entry.Before), Serialize(entry.After), ct);

    private static string? Serialize(object? value) => value is null ? null : JsonSerializer.Serialize(value);
}
