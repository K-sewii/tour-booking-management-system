namespace DongNamTrip.Modules.Administration;

/// <summary>Ngày nghiệp vụ theo giờ Việt Nam (UTC+7), không phụ thuộc múi giờ máy chủ; CSDL lưu UTC.</summary>
internal static class BusinessDate
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public static DateTime Today => (DateTime.UtcNow + Offset).Date;

    /// <summary>Chặn giá trị cực biên (DateTime.MaxValue làm AddDays tràn, năm 0001 làm SQL lỗi).</summary>
    public static bool InRange(DateTime value) => value.Year is >= 1900 and <= 2200;

    public static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value - Offset, DateTimeKind.Utc);

    /// <summary>Cận trên loại trừ: ngày không kèm giờ nghĩa là hết ngày đó.</summary>
    public static DateTime EndExclusiveUtc(DateTime to) => ToUtc(to.TimeOfDay == TimeSpan.Zero ? to.AddDays(1) : to);
}
