using System.ComponentModel.DataAnnotations;

namespace DongNamTrip.Contracts;

// ---- UC03/UC04 · SCR06, SCR07: khách xem địa điểm đã công bố ----
public record SlotInfo(int Id, int ActivityId, DateOnly Date, TimeOnly Start, TimeOnly End, string Status);

public record ActivityInfo(int Id, int DestinationId, string Name, string? Description, int DurationMinutes,
    string? Conditions, decimal? ReferenceFee, string Status, List<SlotInfo> Slots);

/// <summary>Chi tiết địa điểm; gồm mọi trường của <see cref="Destination"/> nên URL cũ vẫn tương thích.</summary>
public record DestinationDetail(int Id, string Name, string Region, string Category, string Address, string Description,
    int VisitMinutes, string Illustration, string Status, TimeOnly? OpenTime, TimeOnly? CloseTime, decimal? Latitude, decimal? Longitude,
    List<ActivityInfo> Activities, string? ImageUrl = null);

// ---- UC14 · SCR14: quản trị địa điểm ----
/// <summary>Một dòng quản trị địa điểm; gồm mọi trường của <see cref="Destination"/>.</summary>
public record AdminDestination(int Id, string Name, string Region, string Category, string Address, string Description,
    int VisitMinutes, string Illustration, string Status, TimeOnly? OpenTime, TimeOnly? CloseTime, decimal? Latitude, decimal? Longitude,
    int ActivityCount, int NarrationCount, bool HasTransactions);

public class DestinationForm : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập tên địa điểm."), StringLength(120, ErrorMessage = "Tên tối đa 120 ký tự.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn khu vực.")]
    public string Region { get; set; } = CatalogOptions.Regions[0];

    [Required(ErrorMessage = "Vui lòng chọn loại hình.")]
    public string Category { get; set; } = CatalogOptions.Categories[0];

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ."), StringLength(250, ErrorMessage = "Địa chỉ tối đa 250 ký tự.")]
    public string Address { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mô tả."), StringLength(3000, ErrorMessage = "Mô tả tối đa 3000 ký tự.")]
    public string Description { get; set; } = "";

    [Range(15, 1440, ErrorMessage = "Thời lượng tham quan từ 15 đến 1440 phút.")]
    public int VisitMinutes { get; set; } = 90;

    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }

    [Range(-90, 90, ErrorMessage = "Vĩ độ từ -90 đến 90.")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Kinh độ từ -180 đến 180.")]
    public decimal? Longitude { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!CatalogOptions.Regions.Contains(Region))
            yield return new ValidationResult("Khu vực không nằm trong phạm vi đề tài.", [nameof(Region)]);
        if (!CatalogOptions.Categories.Contains(Category))
            yield return new ValidationResult("Loại hình không hợp lệ.", [nameof(Category)]);
        if (OpenTime is { } open && CloseTime is { } close && close <= open)
            yield return new ValidationResult("Giờ đóng cửa phải sau giờ mở cửa.", [nameof(CloseTime)]);
        if ((Latitude is null) != (Longitude is null))
            yield return new ValidationResult("Nhập đủ cả vĩ độ và kinh độ hoặc để trống cả hai.", [nameof(Latitude)]);
        if (Name.Length > 0 && string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Vui lòng nhập tên địa điểm.", [nameof(Name)]);
    }
}

public record StatusChange([property: Required] string Status);

public record DestinationOption(int Id, string Name, string Status);

public record DeleteResult(bool Deleted, string Message);

// ---- UC15 · SCR15: hoạt động và suất ----
public record AdminActivity(int Id, int DestinationId, string DestinationName, string Name, string? Description,
    int DurationMinutes, string? Conditions, decimal? ReferenceFee, string Status, List<SlotInfo> Slots);

public class ActivityForm
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn địa điểm.")]
    public int DestinationId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên hoạt động."), StringLength(150, ErrorMessage = "Tên tối đa 150 ký tự.")]
    public string Name { get; set; } = "";

    [StringLength(2000, ErrorMessage = "Mô tả tối đa 2000 ký tự.")]
    public string? Description { get; set; }

    [Range(1, 1440, ErrorMessage = "Thời lượng từ 1 đến 1440 phút.")]
    public int DurationMinutes { get; set; } = 60;

    [StringLength(500, ErrorMessage = "Điều kiện tối đa 500 ký tự.")]
    public string? Conditions { get; set; }

    [Range(0, 1_000_000_000, ErrorMessage = "Phí tham khảo không được âm.")]
    public decimal? ReferenceFee { get; set; }
}

public class SlotForm : IValidatableObject
{
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    public TimeOnly Start { get; set; } = new(9, 0);
    public TimeOnly End { get; set; } = new(10, 0);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (End <= Start)
            yield return new ValidationResult("Giờ kết thúc phải sau giờ bắt đầu.", [nameof(End)]);
    }
}

/// <summary>Kết quả đổi trạng thái có thể ảnh hưởng hồ sơ: <see cref="AffectedItineraries"/> là số lịch trình đang dùng suất/hoạt động.</summary>
public record ImpactResult(string Status, int AffectedItineraries, string Message);

// ---- UC21/UC22 · SCR20: thuyết minh, bản dịch, audio ----
public record LanguageInfo(string Code, string Name);

public record AudioInfo(int Id, int TranslationId, string Voice, string Path, string Status, DateTime? PublishedAt);

public record TranslationInfo(int Id, int NarrationId, string LanguageCode, string LanguageName, string Content,
    string Status, List<AudioInfo> Audios);

public record NarrationInfo(int Id, int DestinationId, string DestinationName, string Title, string Source,
    int Version, string Status, List<TranslationInfo> Translations);

public class NarrationForm
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn địa điểm.")]
    public int DestinationId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề."), StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập nội dung thuyết minh."), StringLength(20000, ErrorMessage = "Nội dung tối đa 20000 ký tự.")]
    public string Source { get; set; } = "";
}

/// <summary>Chỉ dùng cho việc tạo phiên bản mới từ một thuyết minh đã có (không cần chọn lại địa điểm/tiêu đề).</summary>
public class NewVersionForm
{
    [Required(ErrorMessage = "Vui lòng nhập nội dung thuyết minh."), StringLength(20000, ErrorMessage = "Nội dung tối đa 20000 ký tự.")]
    public string Source { get; set; } = "";
}

public class TranslationForm
{
    [Required(ErrorMessage = "Vui lòng chọn ngôn ngữ."), StringLength(10)]
    public string LanguageCode { get; set; } = "vi";

    [Required(ErrorMessage = "Vui lòng nhập nội dung bản dịch."), StringLength(20000, ErrorMessage = "Nội dung tối đa 20000 ký tự.")]
    public string Content { get; set; } = "";
}

/// <summary>Kết quả TTS do ứng dụng quản trị sinh: tệp WAV (Base64) hoặc thông báo lỗi để lưu trạng thái Failed.</summary>
public class AudioUpload
{
    [Required, StringLength(100)]
    public string Voice { get; set; } = "";

    public string? WavBase64 { get; set; }
    public string? Error { get; set; }
}

// ---- UC23 · SCR11: nghe thuyết minh ----
public record ListenTrack(int AudioId, int DestinationId, string DestinationName, string LanguageCode, string LanguageName,
    string Title, string Voice, string Transcript);

public record ListenCatalog(bool HasAccess, DateTime? ValidUntil, string Message, List<ListenTrack> Tracks);

/// <summary>Cấp quyền nghe cho một đơn đã xác nhận. Đức gọi khi xác nhận/hủy đơn (xem ghi chú phối hợp trong Phân_công_code.xlsx).</summary>
public record ListenRightGrant(int OrderId, int AccountId, DateTime From, DateTime To);
