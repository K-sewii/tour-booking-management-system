using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu thông tin đăng nhập, liên hệ và trạng thái tài khoản.
/// </summary>
public partial class Taikhoan
{
    public int MaTk { get; set; }

    public string HoTen { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string MatKhauBam { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public virtual ICollection<Dondat> Dondat { get; set; } = new List<Dondat>();

    public virtual ICollection<Giaodichtien> Giaodichtien { get; set; } = new List<Giaodichtien>();

    public virtual ICollection<Lichtrinh> Lichtrinh { get; set; } = new List<Lichtrinh>();

    public virtual ICollection<Nhatkythaotac> Nhatkythaotac { get; set; } = new List<Nhatkythaotac>();

    public virtual ICollection<Quyennghe> Quyennghe { get; set; } = new List<Quyennghe>();

    public virtual ICollection<TaikhoanVaitro> TaikhoanVaitro { get; set; } = new List<TaikhoanVaitro>();
}
