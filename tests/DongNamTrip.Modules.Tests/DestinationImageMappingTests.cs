using DongNamTrip.Modules.Catalog;
using Xunit;

namespace DongNamTrip.Modules.Tests;

public class DestinationImageMappingTests
{
    [Theory]
    [InlineData("Dinh Độc Lập", "TP. Hồ Chí Minh", "/images/destinations/hcm01-dinh-doc-lap.webp")]
    [InlineData("Bảo tàng Chứng tích Chiến tranh", "TP. Hồ Chí Minh", "/images/destinations/hcm02-bao-tang-chung-tich-chien-tranh.webp")]
    [InlineData("Bưu điện Trung tâm Sài Gòn", "TP. Hồ Chí Minh", "/images/destinations/hcm03-buu-dien-trung-tam-sai-gon.webp")]
    [InlineData("Chợ Bến Thành", "TP. Hồ Chí Minh", "/images/destinations/hcm04-cho-ben-thanh.webp")]
    [InlineData("Địa đạo Củ Chi", "TP. Hồ Chí Minh", "/images/destinations/hcm05-dia-dao-cu-chi.webp")]
    [InlineData("Núi Bà Đen", "Tây Ninh", "/images/destinations/tn01-nui-ba-den.webp")]
    [InlineData("Tòa Thánh Cao Đài Tây Ninh", "Tây Ninh", "/images/destinations/tn02-toa-thanh-cao-dai.webp")]
    [InlineData("Hồ Dầu Tiếng", "Tây Ninh", "/images/destinations/tn03-ho-dau-tieng.webp")]
    [InlineData("Vườn quốc gia Lò Gò - Xa Mát", "Tây Ninh", "/images/destinations/tn04-lo-go-xa-mat.webp")]
    [InlineData("Chùa Gò Kén", "Tây Ninh", "/images/destinations/tn05-chua-go-ken.webp")]
    [InlineData("Bãi Sau Vũng Tàu", "Vũng Tàu", "/images/destinations/vt01-bai-sau-vung-tau.webp")]
    [InlineData("Bạch Dinh", "Vũng Tàu", "/images/destinations/vt02-bach-dinh.webp")]
    [InlineData("Hải đăng Vũng Tàu", "Vũng Tàu", "/images/destinations/vt03-hai-dang-vung-tau.webp")]
    [InlineData("Tượng Chúa Kitô Vua Vũng Tàu", "Vũng Tàu", "/images/destinations/vt04-tuong-chua-kito-vua.webp")]
    [InlineData("Mũi Nghinh Phong", "Vũng Tàu", "/images/destinations/vt05-mui-nghinh-phong.webp")]
    [InlineData("Tháp Tam Thắng", "Vũng Tàu", "/images/destinations/vt06-thap-tam-thang.webp")]
    [InlineData("Chùa Đại Tòng Lâm", "Vũng Tàu", "/images/destinations/vt07-chua-dai-tong-lam.webp")]
    [InlineData("Chợ Bà Rịa", "Vũng Tàu", "/images/destinations/vt08-cho-ba-ria.webp")]
    [InlineData("Suối khoáng nóng Bình Châu", "Vũng Tàu", "/images/destinations/vt09-suoi-khoang-nong-binh-chau.webp")]
    [InlineData("Biển Hồ Cốc", "Vũng Tàu", "/images/destinations/vt10-bien-ho-coc.webp")]
    [InlineData("Khu du lịch Bửu Long", "Đồng Nai", "/images/destinations/dn01-khu-du-lich-buu-long.webp")]
    [InlineData("Văn miếu Trấn Biên", "Đồng Nai", "/images/destinations/dn02-van-mieu-tran-bien.webp")]
    [InlineData("Vườn quốc gia Cát Tiên", "Đồng Nai", "/images/destinations/dn03-vuon-quoc-gia-cat-tien.webp")]
    [InlineData("Khu du lịch thác Giang Điền", "Đồng Nai", "/images/destinations/dn04-thac-giang-dien.webp")]
    [InlineData("Khu du lịch Vườn Xoài", "Đồng Nai", "/images/destinations/dn05-khu-du-lich-vuon-xoai.webp")]
    [InlineData("Chùa Hội Khánh", "Bình Dương", "/images/destinations/bd01-chua-hoi-khanh.webp")]
    [InlineData("Chùa Bà Thiên Hậu Thủ Dầu Một", "Bình Dương", "/images/destinations/bd02-chua-ba-thien-hau.webp")]
    [InlineData("Nhà cổ Trần Văn Hổ", "Bình Dương", "/images/destinations/bd03-nha-co-tran-van-ho.webp")]
    [InlineData("Làng sơn mài Tương Bình Hiệp", "Bình Dương", "/images/destinations/bd04-lang-son-mai-tuong-binh-hiep.webp")]
    [InlineData("Khu du lịch Đại Nam", "Bình Dương", "/images/destinations/bd05-khu-du-lich-dai-nam.webp")]
    public void EveryPackDestinationResolvesByNameAndRegion(string name, string region, string expected)
    {
        Assert.Equal(expected, DestinationImageCatalog.Resolve(null, name, region));
        Assert.Null(DestinationImageCatalog.Resolve(null, name, "Other region"));
    }
}
