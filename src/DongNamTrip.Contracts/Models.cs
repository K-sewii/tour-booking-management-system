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
public record DemoUser(int Id, string Name, string Email, string Role);
public record LoginResponse(string Token, DemoUser User);
public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
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
