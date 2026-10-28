using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu các khoản thu và hoàn tiền của đơn, kèm người ghi nhận và mã đối soát.
/// </summary>
public partial class Giaodichtien
{
    public int MaGd { get; set; }

    public int MaDon { get; set; }

    public string LoaiGd { get; set; } = null!;

    public decimal SoTien { get; set; }

    public string MaThamChieu { get; set; } = null!;

    public DateTime ThoiDiem { get; set; }

    public int MaTkghiNhan { get; set; }

    public virtual Dondat MaDonNavigation { get; set; } = null!;

    public virtual Taikhoan MaTkghiNhanNavigation { get; set; } = null!;
}
