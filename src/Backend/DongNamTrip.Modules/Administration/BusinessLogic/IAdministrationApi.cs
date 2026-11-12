using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Administration;

/// <summary>Cổng công khai của Administration cho các module khác.</summary>
public interface IAdministrationApi
{
    /// <summary>Ghi nhật ký thao tác (UC24, BR13). Không ghi mật khẩu, hash hay token vào dữ liệu trước/sau.</summary>
    Task RecordAsync(AuditEntry entry, CancellationToken ct);

    /// <summary>Gửi thông báo cho khách (TAIKHOAN_THONGBAO). Tiêu đề cắt 160, nội dung cắt 500 ký tự.</summary>
    Task NotifyAsync(int accountId, string title, string body, CancellationToken ct);

    /// <summary>Nhật ký của một đối tượng, mới nhất trước.</summary>
    Task<List<AuditRow>> HistoryAsync(string objectType, object objectId, CancellationToken ct);

    /// <summary>Tên hiển thị của các tài khoản để module khác không phải đọc TAIKHOAN.</summary>
    Task<Dictionary<int, string>> AccountNamesAsync(IReadOnlyCollection<int> accountIds, CancellationToken ct);
}
