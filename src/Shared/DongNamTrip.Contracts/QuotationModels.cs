namespace DongNamTrip.Contracts;

// Quotation: yêu cầu tour riêng, xác minh, báo giá (UC09, UC10, UC17, UC18).
public record QuoteRequestForm(int ItineraryId, int Guests, string Contact, string? Needs);
public record QuoteRequestInfo(int Id, int ItineraryId, int VersionId, int VersionNumber, DateTime SentAt, int Guests, string Status, int? CurrentQuoteId);
public record QuoteItemForm(string Description, string Unit, int Quantity, decimal UnitPrice, bool IncludedInPackage);
public record QuoteForm(IReadOnlyList<QuoteItemForm> Items, decimal Deposit, string CancelPolicy, DateTime ValidUntil);
public record QuoteInfo(int Id, int RequestId, int VersionId, decimal Total, decimal Deposit, string CancelPolicy, DateTime ValidUntil, string Status, IReadOnlyList<QuoteItemForm> Items);
public record QuoteRequestDetail(QuoteRequestInfo Request, ItineraryVersion Version, QuoteInfo? Quote, int? OrderId);
public record VerificationForm(string Item, string Provider, DateTime At, string Result, string? Note);
public record VerificationInfo(int Id, string Item, string Provider, DateTime At, string Result, string? Note);
public record AdminQuoteRequestDetail(QuoteRequestInfo Request, string Contact, string? Needs, ItineraryVersion Version, ItineraryCheck Check, IReadOnlyList<VerificationInfo> Verifications, IReadOnlyList<QuoteInfo> Quotes, IReadOnlyList<string> ChangeNotes);
public record QuoteRequestFilter(string? Status, DateTime? From, DateTime? To, int? Page, int? Size);
public record QuoteAccepted(int QuoteId, int OrderId, decimal Amount, decimal Deposit);
public record ChangeRequest(string Note);
