using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Cổng công khai của Catalog cho các module khác.</summary>
public interface ICatalogApi
{
    Task<bool> ExistsAsync(string kind, int id, CancellationToken ct);
    /// <summary>Địa điểm có tồn tại ở bất kỳ trạng thái nào (kể cả bản nháp), dành cho biên tập nội dung.</summary>
    Task<bool> PlaceExistsAsync(int id, CancellationToken ct);
    Task<Dictionary<int, string>> NamesAsync(string kind, IReadOnlyCollection<int> ids, CancellationToken ct);
    /// <summary>Trả về theo đúng thứ tự <paramref name="ids"/>, bỏ qua id không còn hiển thị.</summary>
    Task<List<Destination>> DestinationsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
    Task<PlannerCatalog> PlannerCatalogAsync(CancellationToken ct);
    Task<ItineraryStopDraft[]?> TourTemplateAsync(int tourId, CancellationToken ct);
    /// <summary>Các điểm của tour theo ngày và thứ tự, kể cả khi tour hoặc địa điểm đã ngừng công bố (đơn đã đặt vẫn phải xem được chương trình).</summary>
    Task<List<ProgramStop>> ProgramAsync(int tourId, CancellationToken ct);
    /// <summary>Tên địa điểm ở mọi trạng thái, cho nội dung đã chốt (phiên bản lịch trình, đơn).</summary>
    Task<Dictionary<int, string>> PlaceNamesAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
    Task<List<PlannerSlot>> SlotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
    /// <summary>Suất kèm tên hoạt động và thời lượng (tên khác <see cref="SlotsAsync"/> vì SlotsAsync giữ kiểu PlannerSlot cho Itinerary).</summary>
    Task<List<SlotDetail>> SlotDetailsAsync(IReadOnlyCollection<int> slotIds, CancellationToken ct);
    Task<Dictionary<int, DestinationHours>> HoursAsync(IReadOnlyCollection<int> destinationIds, CancellationToken ct);
    Task<DepartureInfo?> DepartureAsync(int id, CancellationToken ct);
    Task<Dictionary<int, DepartureInfo>> DeparturesAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
    /// <summary>Giữ chỗ nguyên tử; không tự mở giao dịch, chạy trong giao dịch của người gọi nếu có.</summary>
    Task<HoldResult> HoldSeatsAsync(int departureId, int guests, CancellationToken ct);
    /// <summary>Trả chỗ đã giữ; chỉ gọi sau khi đã đổi trạng thái đơn thành công.</summary>
    Task ReleaseSeatsAsync(int departureId, int guests, CancellationToken ct);
}

