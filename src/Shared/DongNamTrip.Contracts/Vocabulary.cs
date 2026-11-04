namespace DongNamTrip.Contracts;

// Giá trị chuỗi thuộc hợp đồng API (nằm trong URL/JSON mà SPA, Web và Admin gửi lên).
// Code backend phải dùng các hằng này, không viết lại chuỗi (tests/architecture/rules.py kiểm tra).

/// <summary>Loại đối tượng danh mục mà tài khoản có thể lưu hoặc xem.</summary>
public static class ItemKinds
{
    public const string Tour = "tour";
    public const string Destination = "destination";
}

/// <summary>Loại bản ghi lịch sử: tìm kiếm hoặc một <see cref="ItemKinds"/> đã xem.</summary>
public static class ActivityKinds
{
    public const string Search = "search";
}

/// <summary>Phạm vi xóa lịch sử: các lượt tìm hoặc các mục đã xem.</summary>
public static class ActivityScopes
{
    public const string Search = "search";
    public const string Viewed = "viewed";
}

public static class PaymentMethods
{
    public const string BankTransfer = "bank_transfer";
    public const string Cash = "cash";

    public static readonly string[] All = [BankTransfer, Cash];

    public static bool IsSupported(string? method) => method is BankTransfer or Cash;
}

/// <summary>Trạng thái dùng chung cho địa điểm, hoạt động, suất, thuyết minh, bản dịch và audio.</summary>
public static class ContentStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Inactive = "Inactive";
    public const string Pending = "Pending";
    public const string Failed = "Failed";
    public const string Approved = "Approved";

    public static string Label(string status) => status switch
    {
        Draft => "Bản nháp",
        Published => "Đã công bố",
        Inactive => "Ngừng công bố",
        Pending => "Chờ duyệt",
        Failed => "Lỗi",
        Approved => "Đã duyệt",
        _ => status
    };
}

/// <summary>Kết quả của thao tác có thể ảnh hưởng hồ sơ khác (<see cref="ImpactResult"/>).</summary>
public static class ImpactStatuses
{
    public const string NeedConfirm = "NeedConfirm";
    public const string Done = "Done";
}
