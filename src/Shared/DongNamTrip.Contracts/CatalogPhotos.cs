using System.Globalization;
using System.Text;

namespace DongNamTrip.Contracts;

/// <summary>Ảnh đóng gói cùng Web, nhận diện bằng tên và khu vực thay vì ID riêng của từng database.</summary>
public static class CatalogPhotos
{
    private static readonly Dictionary<(string Region, string Name), string> Destinations = new()
    {
        [Key("TP. Hồ Chí Minh", "Dinh Độc Lập")] = "hcm01-dinh-doc-lap.webp",
        [Key("TP. Hồ Chí Minh", "Bảo tàng Chứng tích Chiến tranh")] = "hcm02-bao-tang-chung-tich-chien-tranh.webp",
        [Key("TP. Hồ Chí Minh", "Bưu điện Trung tâm Sài Gòn")] = "hcm03-buu-dien-trung-tam-sai-gon.webp",
        [Key("TP. Hồ Chí Minh", "Chợ Bến Thành")] = "hcm04-cho-ben-thanh.webp",
        [Key("TP. Hồ Chí Minh", "Địa đạo Củ Chi")] = "hcm05-dia-dao-cu-chi.webp",
        [Key("Tây Ninh", "Núi Bà Đen")] = "tn01-nui-ba-den.webp",
        [Key("Tây Ninh", "Tòa Thánh Cao Đài Tây Ninh")] = "tn02-toa-thanh-cao-dai.webp",
        [Key("Tây Ninh", "Hồ Dầu Tiếng")] = "tn03-ho-dau-tieng.webp",
        [Key("Tây Ninh", "Vườn quốc gia Lò Gò - Xa Mát")] = "tn04-lo-go-xa-mat.webp",
        [Key("Tây Ninh", "Chùa Gò Kén")] = "tn05-chua-go-ken.webp",
        [Key("Vũng Tàu", "Bãi Sau Vũng Tàu")] = "vt01-bai-sau-vung-tau.webp",
        [Key("Vũng Tàu", "Bạch Dinh")] = "vt02-bach-dinh.webp",
        [Key("Vũng Tàu", "Hải đăng Vũng Tàu")] = "vt03-hai-dang-vung-tau.webp",
        [Key("Vũng Tàu", "Tượng Chúa Kitô Vua Vũng Tàu")] = "vt04-tuong-chua-kito-vua.webp",
        [Key("Vũng Tàu", "Mũi Nghinh Phong")] = "vt05-mui-nghinh-phong.webp",
        [Key("Vũng Tàu", "Tháp Tam Thắng")] = "vt06-thap-tam-thang.webp",
        [Key("Vũng Tàu", "Chùa Đại Tòng Lâm")] = "vt07-chua-dai-tong-lam.webp",
        [Key("Vũng Tàu", "Chợ Bà Rịa")] = "vt08-cho-ba-ria.jpg",
        [Key("Vũng Tàu", "Suối khoáng nóng Bình Châu")] = "vt09-suoi-khoang-nong-binh-chau.webp",
        [Key("Vũng Tàu", "Biển Hồ Cốc")] = "vt10-bien-ho-coc.webp",
        [Key("Đồng Nai", "Khu du lịch Bửu Long")] = "dn01-khu-du-lich-buu-long.webp",
        [Key("Đồng Nai", "Văn miếu Trấn Biên")] = "dn02-van-mieu-tran-bien.webp",
        [Key("Đồng Nai", "Vườn quốc gia Cát Tiên")] = "dn03-vuon-quoc-gia-cat-tien.webp",
        [Key("Đồng Nai", "Khu du lịch thác Giang Điền")] = "dn04-thac-giang-dien.webp",
        [Key("Đồng Nai", "Khu du lịch Vườn Xoài")] = "dn05-khu-du-lich-vuon-xoai.webp",
        [Key("Bình Dương", "Chùa Hội Khánh")] = "bd01-chua-hoi-khanh.webp",
        [Key("Bình Dương", "Chùa Bà Thiên Hậu Thủ Dầu Một")] = "bd02-chua-ba-thien-hau.jpg",
        [Key("Bình Dương", "Nhà cổ Trần Văn Hổ")] = "bd03-nha-co-tran-van-ho.webp",
        [Key("Bình Dương", "Làng sơn mài Tương Bình Hiệp")] = "bd04-lang-son-mai-tuong-binh-hiep.webp",
        [Key("Bình Dương", "Khu du lịch Đại Nam")] = "bd05-khu-du-lich-dai-nam.webp",
    };

    public static string? DestinationPhoto(string name, string region) =>
        Destinations.TryGetValue(Key(region, name), out var file) ? "/images/destinations/" + file : null;

    public static string DestinationImage(string name, string region) =>
        DestinationPhoto(name, region) ?? "/images/" + CatalogOptions.IllustrationFor(region) + ".svg";

    public static string RegionImage(string region) => region switch
    {
        "TP. Hồ Chí Minh" => DestinationImage("Bưu điện Trung tâm Sài Gòn", region),
        "Tây Ninh" => DestinationImage("Núi Bà Đen", region),
        "Vũng Tàu" => DestinationImage("Bãi Sau Vũng Tàu", region),
        "Đồng Nai" => DestinationImage("Khu du lịch Bửu Long", region),
        "Bình Dương" => DestinationImage("Chùa Hội Khánh", region),
        _ => "/images/" + CatalogOptions.IllustrationFor(region) + ".svg"
    };

    private static (string Region, string Name) Key(string region, string name) => (Normalize(region), Normalize(name));

    private static string Normalize(string value)
    {
        var result = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            var letter = char.ToLowerInvariant(character);
            if (char.IsLetterOrDigit(letter)) result.Append(letter == 'đ' ? 'd' : letter);
        }
        return result.ToString();
    }
}
