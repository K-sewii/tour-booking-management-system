using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Narration;

/// <summary>Cổng công khai của Narration cho các module khác (Booking gọi khi xác nhận/hủy đơn).</summary>
public interface INarrationApi
{
    /// <summary>Cấp (hoặc gia hạn) quyền nghe cho đơn đã xác nhận. Gọi lại với cùng đơn sẽ cập nhật thời hạn, không tạo bản trùng.</summary>
    Task<OpResult<ListenRightGrant>> GrantListenRightAsync(ListenRightGrant grant, int actorId, CancellationToken ct);

    /// <summary>Thu hồi mọi quyền nghe của đơn (hủy đơn). Không có quyền nào cũng không lỗi.</summary>
    Task RevokeListenRightAsync(int orderId, int actorId, CancellationToken ct);
}
