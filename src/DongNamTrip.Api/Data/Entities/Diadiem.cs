using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu thông tin địa điểm, vị trí, thời lượng và trạng thái công bố.
/// </summary>
public partial class Diadiem
{
    public int MaDd { get; set; }

    public string TenDd { get; set; } = null!;

    public string KhuVuc { get; set; } = null!;

    public string LoaiHinh { get; set; } = null!;

    public string DiaChi { get; set; } = null!;

    public decimal? ViDo { get; set; }

    public decimal? KinhDo { get; set; }

    public string MoTa { get; set; } = null!;

    public int ThoiLuongPhut { get; set; }

    public TimeOnly? GioMoCua { get; set; }

    public TimeOnly? GioDongCua { get; set; }

    public string? AnhMinhHoa { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<Hoatdong> Hoatdong { get; set; } = new List<Hoatdong>();

    public virtual ICollection<LichtrinhDiadiem> LichtrinhDiadiem { get; set; } = new List<LichtrinhDiadiem>();

    public virtual ICollection<LichtrinhSuat> LichtrinhSuat { get; set; } = new List<LichtrinhSuat>();

    public virtual ICollection<Thuyetminh> Thuyetminh { get; set; } = new List<Thuyetminh>();

    public virtual ICollection<TourDiadiem> TourDiadiem { get; set; } = new List<TourDiadiem>();
}
