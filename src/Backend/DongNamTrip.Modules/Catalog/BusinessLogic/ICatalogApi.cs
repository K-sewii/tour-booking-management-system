using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Cổng công khai của Catalog cho các module khác.</summary>
public interface ICatalogApi
{
    Task<bool> ExistsAsync(string kind, int id, CancellationToken ct);
    Task<Dictionary<int, string>> NamesAsync(string kind, IReadOnlyCollection<int> ids, CancellationToken ct);
    /// <summary>Trả về theo đúng thứ tự <paramref name="ids"/>, bỏ qua id không còn hiển thị.</summary>
    Task<List<Destination>> DestinationsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
}
