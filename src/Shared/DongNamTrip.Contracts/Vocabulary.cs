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

/// <summary>Trạng thái tour dùng lại <see cref="ContentStatus"/> (Draft, Published, Inactive).</summary>
public static class TourStatus
{
    public const string Draft = ContentStatus.Draft;
    public const string Published = ContentStatus.Published;
    public const string Inactive = ContentStatus.Inactive;
}

/// <summary>LICHKHOIHANH.TrangThai (STM-06).</summary>
public static class DepartureStatus
{
    public const string Open = "Open";
    public const string Closed = "Closed";
    public const string Inactive = "Inactive";

    public static readonly string[] All = [Open, Closed, Inactive];

    public static string Label(string status) => status switch
    {
        Open => "Mở",
        Closed => "Đóng",
        Inactive => "Ngừng",
        _ => status
    };
}

/// <summary>DONDAT.TrangThai (STM-07).</summary>
public static class OrderStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string CancelRequested = "CancelRequested";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
    public const string Completed = "Completed";

    public static readonly string[] All = [Pending, Confirmed, CancelRequested, Cancelled, Expired, Completed];

    public static string Label(string status) => status switch
    {
        Pending => "Chờ xử lý",
        Confirmed => "Đã xác nhận",
        CancelRequested => "Chờ hủy",
        Cancelled => "Đã hủy",
        Expired => "Hết hạn",
        Completed => "Hoàn thành",
        _ => status
    };
}

/// <summary>Nguồn đơn (dẫn xuất từ MaLKH hoặc MaBG, không lưu cột).</summary>
public static class BookingSources
{
    public const string Departure = "Departure";
    public const string Quote = "Quote";

    public static readonly string[] All = [Departure, Quote];

    public static string Label(string source) => source switch
    {
        Departure => "Tour có sẵn",
        Quote => "Tour riêng",
        _ => source
    };
}

/// <summary>GIAODICHTIEN.LoaiGD.</summary>
public static class MoneyKinds
{
    public const string Receipt = "Receipt";
    public const string Refund = "Refund";

    public static readonly string[] All = [Receipt, Refund];

    public static string Label(string kind) => kind switch
    {
        Receipt => "Thu",
        Refund => "Hoàn",
        _ => kind
    };
}

/// <summary>YEUCAUTOURRIENG.TrangThai (STM-04).</summary>
public static class QuoteRequestStatus
{
    public const string Submitted = "Submitted";
    public const string Verifying = "Verifying";
    public const string Quoted = "Quoted";
    public const string Accepted = "Accepted";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Submitted, Verifying, Quoted, Accepted, Cancelled];

    public static string Label(string status) => status switch
    {
        Submitted => "Chờ tiếp nhận",
        Verifying => "Đang xác minh",
        Quoted => "Đã báo giá",
        Accepted => "Đã chốt",
        Cancelled => "Đã hủy",
        _ => status
    };
}

/// <summary>BAOGIA.TrangThai (STM-08).</summary>
public static class QuoteStatus
{
    public const string Sent = "Sent";
    public const string Accepted = "Accepted";
    public const string Expired = "Expired";
    public const string Superseded = "Superseded";

    public static readonly string[] All = [Sent, Accepted, Expired, Superseded];

    public static string Label(string status) => status switch
    {
        Sent => "Đã gửi",
        Accepted => "Đã chấp nhận",
        Expired => "Hết hạn",
        Superseded => "Đã thay thế",
        _ => status
    };
}

/// <summary>XACMINHDICHVU.KetQua.</summary>
public static class VerificationResults
{
    public const string Available = "Available";
    public const string Unavailable = "Unavailable";
    public const string Substitute = "Substitute";

    public static readonly string[] All = [Available, Unavailable, Substitute];

    public static string Label(string result) => result switch
    {
        Available => "Đáp ứng",
        Unavailable => "Không đáp ứng",
        Substitute => "Thay thế",
        _ => result
    };
}

/// <summary>TAIKHOAN.TrangThai.</summary>
public static class AccountStatus
{
    public const string Active = "Active";
    public const string Locked = "Locked";

    public static readonly string[] All = [Active, Locked];

    public static string Label(string status) => status switch
    {
        Active => "Hoạt động",
        Locked => "Khóa",
        _ => status
    };
}

/// <summary>Điều kiện hủy mặc định ghi vào DONDAT.DieuKienHuy khi tour chưa có.</summary>
public static class CancelPolicyDefaults
{
    public const string Standard = "Áp dụng điều kiện hủy chung của DongNamTrip; nhân viên xác nhận khoản hoàn.";
}
