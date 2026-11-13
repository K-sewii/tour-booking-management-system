using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Itinerary;

/// <summary>
/// Kiểm tra xung đột lịch (UC07). Lớp thuần, không truy cập dữ liệu nên kiểm thử đơn vị được.
/// Thời gian di chuyển giữa các chặng chưa có dữ liệu nên tính 0.
/// </summary>
public sealed class ScheduleChecker(int bufferMinutes = 30)
{
    private const string TravelNote = " (chưa tính thời gian di chuyển giữa các chặng)";

    public ItineraryCheck Check(DateOnly startDate, IReadOnlyList<StopPlan> stops, IReadOnlyList<SlotDetail> slots, IReadOnlyList<SlotPick> picks)
    {
        var warnings = new List<ScheduleWarning>();
        var byId = slots.GroupBy(slot => slot.Id).ToDictionary(group => group.Key, group => group.First());
        var stopOf = stops.GroupBy(stop => stop.DestinationId).ToDictionary(group => group.Key, group => group.First());

        foreach (var group in picks.GroupBy(pick => (pick.DestinationId, pick.SlotId)).Where(group => group.Count() > 1))
            warnings.Add(new(ScheduleWarningCodes.Duplicate, group.Key.DestinationId, group.Key.SlotId, $"Suất #{group.Key.SlotId} được chọn nhiều lần cho cùng một điểm."));

        var chosen = new List<SlotDetail>();
        foreach (var pick in picks.DistinctBy(pick => (pick.DestinationId, pick.SlotId)))
        {
            if (!byId.TryGetValue(pick.SlotId, out var slot))
            {
                warnings.Add(new(ScheduleWarningCodes.Inactive, pick.DestinationId, pick.SlotId, $"Suất #{pick.SlotId} không còn tồn tại."));
                continue;
            }
            chosen.Add(slot);
            if (slot.Status != ContentStatus.Published)
                warnings.Add(new(ScheduleWarningCodes.Inactive, pick.DestinationId, slot.Id, $"Suất #{slot.Id} đã ngừng công bố."));
            if (!stopOf.TryGetValue(pick.DestinationId, out var stop)) continue;
            if (WrongDate(startDate, stop, slot))
                warnings.Add(new(ScheduleWarningCodes.WrongDate, pick.DestinationId, slot.Id, $"Suất #{slot.Id} không đúng ngày thứ {stop.Day}."));
            if (stop.Opens is { } opens && stop.Closes is { } closes && (slot.Start < opens || slot.End > closes))
                warnings.Add(new(ScheduleWarningCodes.OutOfHours, pick.DestinationId, slot.Id, $"Suất #{slot.Id} nằm ngoài giờ mở cửa {opens:HH:mm}–{closes:HH:mm}."));
        }

        foreach (var day in chosen.GroupBy(slot => slot.Date))
        {
            var entries = day.OrderBy(slot => slot.Start).ThenBy(slot => slot.Id).ToArray();
            for (var i = 0; i < entries.Length; i++)
                for (var j = i + 1; j < entries.Length; j++)
                    if (Overlaps(entries[i], entries[j]))
                        warnings.Add(new(ScheduleWarningCodes.Overlap, entries[j].DestinationId, entries[j].Id,
                            $"Các suất #{entries[i].Id} và #{entries[j].Id} trùng giờ hoặc cách nhau dưới {bufferMinutes} phút{TravelNote}."));
        }

        // Chỉ điểm có chọn suất mới cần giờ mở cửa và thời lượng để kết luận.
        var withSlots = picks.Select(pick => pick.DestinationId).ToHashSet();
        foreach (var stop in stops.Where(stop => withSlots.Contains(stop.DestinationId) && (stop.Opens is null || stop.Closes is null || stop.VisitMinutes <= 0)))
            warnings.Add(new(ScheduleWarningCodes.Incomplete, stop.DestinationId, null, $"Điểm #{stop.DestinationId} thiếu giờ mở cửa hoặc thời lượng nên chưa kết luận được."));

        var complete = warnings.All(warning => warning.Code != ScheduleWarningCodes.Incomplete);
        var fits = warnings.All(warning => warning.Code == ScheduleWarningCodes.Incomplete);
        return new(fits, complete, warnings);
    }

    private bool Overlaps(SlotDetail a, SlotDetail b)
    {
        var buffer = TimeSpan.FromMinutes(bufferMinutes);
        return a.Start.ToTimeSpan() < b.End.ToTimeSpan() + buffer && b.Start.ToTimeSpan() < a.End.ToTimeSpan() + buffer;
    }

    private static bool WrongDate(DateOnly startDate, StopPlan stop, SlotDetail slot) => slot.Date != startDate.AddDays(stop.Day - 1);
}
