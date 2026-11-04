namespace DongNamTrip.Modules.Administration;

/// <summary>Cổng công khai của Administration cho các module khác.</summary>
public interface IAdministrationApi
{
    /// <summary>Ghi nhật ký thao tác (UC24, BR13). Không ghi mật khẩu, hash hay token vào dữ liệu trước/sau.</summary>
    Task RecordAsync(AuditEntry entry, CancellationToken ct);
}
