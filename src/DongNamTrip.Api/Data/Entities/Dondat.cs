using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu đơn tour có sẵn hoặc tour riêng cùng giá và điều kiện đã chốt.
/// </summary>
public partial class Dondat
{
    public int MaDon { get; set; }

    public int MaTk { get; set; }

    public int? MaLkh { get; set; }

    public int? MaBg { get; set; }

    public int SoKhach { get; set; }

    public decimal GiaChot { get; set; }

    public string DieuKienHuy { get; set; } = null!;

    public DateTime? HanGiuCho { get; set; }

    public string? LyDoHuy { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayDat { get; set; }

    public virtual ICollection<Giaodichtien> Giaodichtien { get; set; } = new List<Giaodichtien>();

    public virtual Baogia? MaBgNavigation { get; set; }

    public virtual Lichkhoihanh? MaLkhNavigation { get; set; }

    public virtual Taikhoan MaTkNavigation { get; set; } = null!;

    public virtual ICollection<Quyennghe> Quyennghe { get; set; } = new List<Quyennghe>();
}
