using System;
using System.Collections.Generic;

namespace DongNamTrip.Modules.Catalog;

/// <summary>
/// Stable destination photo mapping by (region, name), independent of SQL identity values.
/// Explicit DIADIEM.AnhMinhHoa paths take precedence. Unknown places use SVG in the UI.
/// </summary>
public static class DestinationImageCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Photos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["TP. Hồ Chí Minh|Dinh Độc Lập"] = "/images/destinations/hcm01-dinh-doc-lap.webp",
        ["TP. Hồ Chí Minh|Bảo tàng Chứng tích Chiến tranh"] = "/images/destinations/hcm02-bao-tang-chung-tich-chien-tranh.webp",
        ["TP. Hồ Chí Minh|Bưu điện Trung tâm Sài Gòn"] = "/images/destinations/hcm03-buu-dien-trung-tam-sai-gon.webp",
        ["TP. Hồ Chí Minh|Chợ Bến Thành"] = "/images/destinations/hcm04-cho-ben-thanh.webp",
        ["TP. Hồ Chí Minh|Địa đạo Củ Chi"] = "/images/destinations/hcm05-dia-dao-cu-chi.webp",
        ["Tây Ninh|Núi Bà Đen"] = "/images/destinations/tn01-nui-ba-den.webp",
        ["Tây Ninh|Tòa Thánh Cao Đài Tây Ninh"] = "/images/destinations/tn02-toa-thanh-cao-dai.webp",
        ["Tây Ninh|Hồ Dầu Tiếng"] = "/images/destinations/tn03-ho-dau-tieng.webp",
        ["Tây Ninh|Vườn quốc gia Lò Gò - Xa Mát"] = "/images/destinations/tn04-lo-go-xa-mat.webp",
        ["Tây Ninh|Chùa Gò Kén"] = "/images/destinations/tn05-chua-go-ken.webp",
        ["Vũng Tàu|Bãi Sau Vũng Tàu"] = "/images/destinations/vt01-bai-sau-vung-tau.webp",
        ["Vũng Tàu|Bạch Dinh"] = "/images/destinations/vt02-bach-dinh.webp",
        ["Vũng Tàu|Hải đăng Vũng Tàu"] = "/images/destinations/vt03-hai-dang-vung-tau.webp",
        ["Vũng Tàu|Tượng Chúa Kitô Vua Vũng Tàu"] = "/images/destinations/vt04-tuong-chua-kito-vua.webp",
        ["Vũng Tàu|Mũi Nghinh Phong"] = "/images/destinations/vt05-mui-nghinh-phong.webp",
        ["Vũng Tàu|Tháp Tam Thắng"] = "/images/destinations/vt06-thap-tam-thang.webp",
        ["Vũng Tàu|Chùa Đại Tòng Lâm"] = "/images/destinations/vt07-chua-dai-tong-lam.webp",
        ["Vũng Tàu|Chợ Bà Rịa"] = "/images/destinations/vt08-cho-ba-ria.webp",
        ["Vũng Tàu|Suối khoáng nóng Bình Châu"] = "/images/destinations/vt09-suoi-khoang-nong-binh-chau.webp",
        ["Vũng Tàu|Biển Hồ Cốc"] = "/images/destinations/vt10-bien-ho-coc.webp",
        ["Đồng Nai|Khu du lịch Bửu Long"] = "/images/destinations/dn01-khu-du-lich-buu-long.webp",
        ["Đồng Nai|Văn miếu Trấn Biên"] = "/images/destinations/dn02-van-mieu-tran-bien.webp",
        ["Đồng Nai|Vườn quốc gia Cát Tiên"] = "/images/destinations/dn03-vuon-quoc-gia-cat-tien.webp",
        ["Đồng Nai|Khu du lịch thác Giang Điền"] = "/images/destinations/dn04-thac-giang-dien.webp",
        ["Đồng Nai|Khu du lịch Vườn Xoài"] = "/images/destinations/dn05-khu-du-lich-vuon-xoai.webp",
        ["Bình Dương|Chùa Hội Khánh"] = "/images/destinations/bd01-chua-hoi-khanh.webp",
        ["Bình Dương|Chùa Bà Thiên Hậu Thủ Dầu Một"] = "/images/destinations/bd02-chua-ba-thien-hau.webp",
        ["Bình Dương|Nhà cổ Trần Văn Hổ"] = "/images/destinations/bd03-nha-co-tran-van-ho.webp",
        ["Bình Dương|Làng sơn mài Tương Bình Hiệp"] = "/images/destinations/bd04-lang-son-mai-tuong-binh-hiep.webp",
        ["Bình Dương|Khu du lịch Đại Nam"] = "/images/destinations/bd05-khu-du-lich-dai-nam.webp",
    };

    public static string? Resolve(string? storedUrl, string name, string region)
    {
        if (!string.IsNullOrWhiteSpace(storedUrl))
        {
            var candidate = storedUrl.Trim();
            if (candidate.StartsWith("/images/destinations/", StringComparison.OrdinalIgnoreCase)
                && !candidate.Contains("..", StringComparison.Ordinal)
                && !candidate.Contains('\\')
                && (candidate.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                    || candidate.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    || candidate.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
                return candidate;
        }
        return Photos.TryGetValue($"{region}|{name}", out var url) ? url : null;
    }
}
