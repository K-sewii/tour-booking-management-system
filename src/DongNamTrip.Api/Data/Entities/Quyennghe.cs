using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu quyền nghe theo tài khoản, đơn đã xác nhận và khoảng hiệu lực.
/// </summary>
public partial class Quyennghe
{
    public int MaQn { get; set; }

    public int MaDon { get; set; }

    public int MaTk { get; set; }

    public DateTime HieuLucTu { get; set; }

    public DateTime HieuLucDen { get; set; }

    public virtual Dondat MaDonNavigation { get; set; } = null!;

    public virtual Taikhoan MaTkNavigation { get; set; } = null!;
}
