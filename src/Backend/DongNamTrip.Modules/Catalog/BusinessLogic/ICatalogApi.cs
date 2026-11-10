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
    Task<List<PlannerSlot>> SlotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
}
