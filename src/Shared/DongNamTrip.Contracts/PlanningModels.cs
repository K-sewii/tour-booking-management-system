using System.ComponentModel.DataAnnotations;

namespace DongNamTrip.Contracts;

public record PlannerCatalog(List<Destination> Destinations, List<PlannerActivity> Activities, List<PlannerSlot> Slots);
public record PlannerActivity(int Id, int DestinationId, string Name, int DurationMinutes, string? Conditions, decimal? ReferenceFee);
public record PlannerSlot(int Id, int ActivityId, int DestinationId, DateOnly Date, TimeOnly StartsAt, TimeOnly EndsAt, string Status);

public class PrivateTourRequestDraft
{
    [Required, StringLength(250)] public string Contact { get; set; } = "";
    [StringLength(2000)] public string? ServiceNeeds { get; set; }
}

public record PrivateTourRequest(
    int Id, int ItineraryId, int Version, int CustomerId, string CustomerName,
    string Contact, string? ServiceNeeds, DateTime SubmittedAt, string Status,
    Itinerary Snapshot, List<ServiceVerification> Verifications, List<PrivateTourQuote> Quotes);

public class ServiceVerificationDraft
{
    [Required, StringLength(250)] public string Item { get; set; } = "";
    [Required, StringLength(250)] public string Supplier { get; set; } = "";
    [Required] public string Result { get; set; } = "";
    [StringLength(1000)] public string? Note { get; set; }
}

public record ServiceVerification(int Id, string Item, string Supplier, string Result, string? Note, DateTime CheckedAt);

public class QuoteDraft
{
    [Required] public DateTime ExpiresAt { get; set; }
    [Range(0, 999999999999999999)] public decimal AmountDue { get; set; }
    [Required, StringLength(2000)] public string CancellationTerms { get; set; } = "";
    [Required, MinLength(1)] public QuoteLineDraft[] Lines { get; set; } = [];
}

public class QuoteLineDraft
{
    [Required, StringLength(500)] public string Description { get; set; } = "";
    [Required, StringLength(50)] public string Unit { get; set; } = "";
    [Range(1, 100000)] public int Quantity { get; set; }
    [Range(0, 999999999999999999)] public decimal UnitPrice { get; set; }
}

public record QuoteLine(int Id, string Description, string Unit, int Quantity, decimal UnitPrice);
public record PrivateTourQuote(int Id, int RequestId, int ItineraryVersion, decimal Total, decimal AmountDue,
    string CancellationTerms, DateTime ExpiresAt, string Status, List<QuoteLine> Lines, int? OrderId);
public record PrivateTourOrder(int Id, int QuoteId, int CustomerId, int Guests, decimal AgreedPrice,
    string CancellationTerms, string Status, DateTime BookedAt);
