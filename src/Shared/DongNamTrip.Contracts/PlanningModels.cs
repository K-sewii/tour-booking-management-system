namespace DongNamTrip.Contracts;

public record PlannerCatalog(List<Destination> Destinations, List<PlannerActivity> Activities, List<PlannerSlot> Slots);
public record PlannerActivity(int Id, int DestinationId, string Name, int DurationMinutes, string? Conditions, decimal? ReferenceFee);
public record PlannerSlot(int Id, int ActivityId, int DestinationId, DateOnly Date, TimeOnly StartsAt, TimeOnly EndsAt, string Status);
