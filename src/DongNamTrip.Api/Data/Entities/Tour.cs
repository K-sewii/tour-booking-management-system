using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu chương trình tour mẫu, giá tham khảo và điều kiện hủy.
/// </summary>
public partial class Tour
{
    public int MaTour { get; set; }

    public string TenTour { get; set; } = null!;

    public string KhuVuc { get; set; } = null!;

    public string LoaiHinh { get; set; } = null!;

    public string ThoiLuong { get; set; } = null!;

    public decimal GiaThamKhao { get; set; }

    public string TomTat { get; set; } = null!;

    public string? DieuKienHuy { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<Lichkhoihanh> Lichkhoihanh { get; set; } = new List<Lichkhoihanh>();

    public virtual ICollection<TourDiadiem> TourDiadiem { get; set; } = new List<TourDiadiem>();
}
