namespace DongNamTrip.Modules;

/// <summary>
/// Quy ước giờ duy nhất của backend: CSDL lưu UTC; ngày nghiệp vụ (ngày đi, hạn nghe, ngày hoàn thành) tính theo giờ Việt Nam,
/// UTC+7 cố định (không đổi giờ mùa hè), không phụ thuộc múi giờ máy chủ chạy API. Mọi module dùng chung lớp này,
/// không tự viết lại phép đổi giờ.
/// </summary>
public static class VnTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    /// <summary>Ngày hiện tại theo giờ Việt Nam (00:00).</summary>
    public static DateTime Today => (DateTime.UtcNow + Offset).Date;

    /// <summary>Ngày hiện tại theo giờ Việt Nam, dạng ngày lịch (so với <c>NgayDi</c>).</summary>
    public static DateOnly TodayDate => DateOnly.FromDateTime(Today);

    /// <summary>Chặn giá trị cực biên (DateTime.MaxValue làm AddDays tràn, năm 0001 làm SQL lỗi).</summary>
    public static bool InRange(DateTime value) => value.Year is >= 1900 and <= 2200;

    /// <summary>
    /// Giờ do người dùng nhập (bộ lọc, khoảng ngày) → UTC. Utc giữ nguyên; Local là chuỗi có múi đã được đổi sang giờ máy chủ
    /// nên đổi ngược bằng <c>ToUniversalTime</c>; Unspecified (không kèm múi) được hiểu là giờ Việt Nam.
    /// </summary>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => value <= DateTime.MinValue + Offset
            ? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)
            : DateTime.SpecifyKind(value - Offset, DateTimeKind.Utc),
    };

    /// <summary>Ngày lịch cộng giờ trong ngày (giờ Việt Nam) → UTC; không phụ thuộc múi giờ máy chủ.</summary>
    public static DateTime ToUtc(DateOnly date, TimeOnly timeOfDay) =>
        DateTime.SpecifyKind(date.ToDateTime(timeOfDay) - Offset, DateTimeKind.Utc);

    /// <summary>
    /// Thời điểm tuyệt đối (giá trị từ CSDL hoặc truyền giữa các module) → UTC: Utc giữ nguyên, Local đổi sang UTC,
    /// Unspecified coi là UTC theo hợp đồng nội bộ.
    /// </summary>
    public static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    /// <summary>UTC → giờ Việt Nam, dùng khi hiển thị cho khách (ví dụ ngày bắt đầu quyền nghe).</summary>
    public static DateTime FromUtc(DateTime utc) => DateTime.SpecifyKind(AsUtc(utc) + Offset, DateTimeKind.Unspecified);

    /// <summary>Cận trên loại trừ: ngày không kèm giờ nghĩa là hết ngày đó.</summary>
    public static DateTime EndExclusiveUtc(DateTime to) => ToUtc(to.TimeOfDay == TimeSpan.Zero ? to.AddDays(1) : to);
}
