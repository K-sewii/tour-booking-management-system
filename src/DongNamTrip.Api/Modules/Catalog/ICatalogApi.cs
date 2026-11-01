using DongNamTrip.Contracts;

namespace DongNamTrip.Api.Modules.Catalog;

/// <summary>Cổng công khai của Catalog cho các module khác.</summary>
public interface ICatalogApi
{
    List<Destination> Destinations();
    List<Tour> Tours();
}
