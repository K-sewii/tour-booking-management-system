using System.ComponentModel.DataAnnotations;

namespace DongNamTrip.Contracts;

public record Destination(int Id, string Name, string Region, string Category, string Address, string Description, int VisitMinutes, string Illustration);

public class DestinationDraft
{
    [Required, StringLength(120)]
    public string Name { get; set; } = "";

    [Required]
    public string Region { get; set; } = "TP. Hồ Chí Minh";

    [Required]
    public string Category { get; set; } = "Văn hóa";

    [Required, StringLength(250)]
    public string Address { get; set; } = "";

    [Required, StringLength(3000)]
    public string Description { get; set; } = "";

    [Range(15, 1440)]
    public int VisitMinutes { get; set; } = 90;
}

public record Tour(int Id, string Name, string Region, string Category, string Duration, decimal ReferencePrice, string Summary, string Illustration, int[] DestinationIds);

public record AuthenticatedUser(int Id, string Name, string Email, IReadOnlyList<string> Roles);
public record LoginResponse(string Token, AuthenticatedUser User);

public class LoginRequest
{
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không đúng định dạng."), StringLength(254, ErrorMessage = "Email tối đa 254 ký tự.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), StringLength(128, ErrorMessage = "Mật khẩu tối đa 128 ký tự.")]
    public string Password { get; set; } = "";
}

public class RegisterRequest : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không đúng định dạng."), StringLength(254, ErrorMessage = "Email tối đa 254 ký tự.")]
    public string Email { get; set; } = "";

    [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 chữ số."), RegularExpression(@"^[0-9]+$", ErrorMessage = "Số điện thoại chỉ gồm chữ số.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), StringLength(128, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu."), Compare(nameof(Password), ErrorMessage = "Nhập lại mật khẩu chưa khớp.")]
    public string ConfirmPassword { get; set; } = "";

    [Range(typeof(bool), "true", "true", ErrorMessage = "Vui lòng đồng ý với điều khoản.")]
    public bool TermsAccepted { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(Name) && string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Vui lòng nhập họ tên.", [nameof(Name)]);

        if (!string.IsNullOrEmpty(Password)
            && (!Password.Any(char.IsLetter) || !Password.Any(char.IsDigit)))
            yield return new ValidationResult("Mật khẩu cần có ít nhất một chữ và một số.", [nameof(Password)]);
    }
}

public record AccountProfile(int Id, string Name, string Email, string? PhoneNumber);

public class ChangePasswordRequest : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    public string CurrentPassword { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới."), StringLength(128, MinimumLength = 8, ErrorMessage = "Mật khẩu mới cần từ 8 đến 128 ký tự.")]
    public string NewPassword { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới."), Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu mới chưa khớp.")]
    public string ConfirmNewPassword { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!string.IsNullOrEmpty(NewPassword) && (!NewPassword.Any(char.IsLetter) || !NewPassword.Any(char.IsDigit)))
            yield return new ValidationResult("Mật khẩu mới cần ít nhất một chữ và một số.", [nameof(NewPassword)]);
        if (!string.IsNullOrEmpty(NewPassword) && CurrentPassword == NewPassword)
            yield return new ValidationResult("Mật khẩu mới cần khác mật khẩu hiện tại.", [nameof(NewPassword)]);
    }
}

public record AccountSession(string Id, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, bool IsCurrent);
public record PaymentEntry(int Id, DateTime PlacedAt, string Status, decimal AgreedPrice, int Guests, int? QuoteId, int? DepartureId, IReadOnlyList<MoneyEntry> Transactions);
public record PaymentChoiceDto(string? MethodCode, DateTime? UpdatedAt);
public record PaymentChoiceRequest(string MethodCode);
public record MoneyEntry(int Id, string Kind, decimal Amount, DateTime At, string Reference);
public record AccountCollectionItem(string Kind, int ItemId, string? Name, DateTime At, bool Missing);
public record AccountActivityEntry(long Id, string Kind, int? ItemId, string? Query, string? Name, DateTime At, bool Missing);
public record AccountCollections(IReadOnlyList<AccountCollectionItem> Favorites, IReadOnlyList<AccountActivityEntry> Searches, IReadOnlyList<AccountActivityEntry> Viewed);
public record AccountActivityDraft(string Kind, int? ItemId, string? Query);
public record AccountNoticeEntry(long Id, string Title, string Body, DateTime At, DateTime? ReadAt);
public record AccountPreferenceDto(bool SecurityNotices);
public record AccountNoticesPage(IReadOnlyList<AccountNoticeEntry> Notices, AccountPreferenceDto Preference);

public class UpdateProfileRequest : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên cần có từ 2 đến 100 ký tự.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không đúng định dạng."), StringLength(254, ErrorMessage = "Email tối đa 254 ký tự.")]
    public string Email { get; set; } = "";

    [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 chữ số."), RegularExpression(@"^[0-9]+$", ErrorMessage = "Số điện thoại chỉ gồm chữ số.")]
    public string? PhoneNumber { get; set; }

    public void Normalize()
    {
        Name = Name?.Trim() ?? "";
        Email = Email?.Trim().ToLowerInvariant() ?? "";
        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim();
    }

    public IReadOnlyList<ValidationResult> ValidateForExistingPhone(string? existingPhone)
    {
        var results = new List<ValidationResult>();
        var submittedPhone = PhoneNumber;
        // Existing formatted numbers can remain unchanged; every newly entered number follows SCR03.
        PhoneNumber = null;
        try { Validator.TryValidateObject(this, new ValidationContext(this), results, true); }
        finally { PhoneNumber = submittedPhone; }
        if (submittedPhone is not null
            && !string.Equals(submittedPhone, existingPhone?.Trim(), StringComparison.Ordinal)
            && (submittedPhone.Length > 15 || !submittedPhone.All(character => character is >= '0' and <= '9')))
            results.Add(new ValidationResult(submittedPhone.Length > 15 ? "Số điện thoại tối đa 15 chữ số." : "Số điện thoại chỉ gồm chữ số.", [nameof(PhoneNumber)]));
        return results;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Email.Any(character => character > 127))
            yield return new ValidationResult("Email không đúng định dạng.", [nameof(Email)]);
    }
}

public class ItineraryDraft
{
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

    [Range(1, 40)]
    public int Guests { get; set; } = 2;

    [Required, MinLength(1), MaxLength(12)]
    public int[] DestinationIds { get; set; } = [];
}

public record Itinerary(int Id, int UserId, string Name, DateOnly StartDate, int Guests, int[] DestinationIds);

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
    public const string Operator = "Operator";
    public const string Editor = "Editor";
    public const string Finance = "Finance";
    public const string Publisher = "Publisher";

    public static readonly string[] Staff = [Admin, Operator, Editor, Finance, Publisher];

    public static bool HasAny(IEnumerable<string> roles, params string[] allowed) =>
        roles.Any(role => allowed.Contains(role, StringComparer.OrdinalIgnoreCase));
}

public static class CatalogOptions
{
    // Nhãn khu vực du lịch theo phạm vi đề tài, không phải danh mục hành chính.
    public static readonly string[] Regions = ["TP. Hồ Chí Minh", "Tây Ninh", "Vũng Tàu", "Đồng Nai", "Bình Dương"];
    public static readonly string[] Categories = ["Văn hóa", "Thiên nhiên", "Biển", "Ẩm thực", "Kết hợp"];

    public static string IllustrationFor(string region) => region switch
    {
        "Tây Ninh" => "mountain",
        "Vũng Tàu" => "coast",
        "Đồng Nai" => "forest",
        "Bình Dương" => "village",
        _ => "city"
    };
}
