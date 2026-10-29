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

    [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 chữ số."), RegularExpression(@"^\d+$", ErrorMessage = "Số điện thoại chỉ gồm chữ số.")]
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

public class UpdateProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [StringLength(15), RegularExpression(@"^\+?[0-9]{9,14}$", ErrorMessage = "Số điện thoại cần có 9–14 chữ số, có thể bắt đầu bằng dấu +.")]
    public string? PhoneNumber { get; set; }
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
