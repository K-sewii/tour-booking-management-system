using DongNamTrip.Modules.Catalog;
using Xunit;

namespace DongNamTrip.Modules.Tests;

public class DestinationImageCatalogTests
{
    [Fact]
    public void Known_Destination_Resolves_Without_Database_Image()
    {
        Assert.Equal("/images/destinations/hcm04-cho-ben-thanh.webp",
            DestinationImageCatalog.Resolve(null, "Chợ Bến Thành", "TP. Hồ Chí Minh"));
    }

    [Fact]
    public void Database_Image_Overrides_Static_Map()
    {
        Assert.Equal("/images/destinations/custom.webp",
            DestinationImageCatalog.Resolve("/images/destinations/custom.webp", "Chợ Bến Thành", "TP. Hồ Chí Minh"));
    }

    [Fact]
    public void External_Image_Is_Not_Trusted()
    {
        Assert.Equal("/images/destinations/hcm04-cho-ben-thanh.webp",
            DestinationImageCatalog.Resolve("https://other-site.invalid/photo.jpg", "Chợ Bến Thành", "TP. Hồ Chí Minh"));
    }

    [Fact]
    public void Unknown_Destination_Uses_UI_Fallback()
    {
        Assert.Null(DestinationImageCatalog.Resolve(null, "Chưa có ảnh", "Không có khu vực"));
    }
}
