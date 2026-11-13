namespace DongNamTrip.Contracts;

// Itinerary: kiểm tra xung đột và phiên bản lịch trình (UC06-UC08). Điểm dừng và suất vẫn nằm trong ItineraryStopDraft.SlotIds.
public record SlotPick(int DestinationId, int SlotId, string? Note);
public record DraftCheckRequest(ItineraryDraft Draft, IReadOnlyList<SlotPick>? Picks = null);
public record StopPlan(int DestinationId, int Day, int Order, int VisitMinutes, TimeOnly? Opens, TimeOnly? Closes);
/// <summary>Code: Overlap | WrongDate | Inactive | Duplicate | OutOfHours | Incomplete (xem <see cref="ScheduleWarningCodes"/>).</summary>
public record ScheduleWarning(string Code, int? DestinationId, int? SlotId, string Message);
public record ItineraryCheck(bool Fits, bool Complete, IReadOnlyList<ScheduleWarning> Warnings);
public record ItineraryVersionRef(int VersionId, int Number, DateTime CreatedAt);
public record ItineraryVersion(int VersionId, int ItineraryId, int AccountId, int Number, DateOnly StartDate, int Guests, string ContentJson);

public static class ScheduleWarningCodes
{
    public const string Overlap = "Overlap";
    public const string WrongDate = "WrongDate";
    public const string Inactive = "Inactive";
    public const string Duplicate = "Duplicate";
    public const string OutOfHours = "OutOfHours";
    public const string Incomplete = "Incomplete";
}
