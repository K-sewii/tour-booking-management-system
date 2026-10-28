using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu giá và điều kiện gắn với yêu cầu cùng phiên bản lịch trình được báo giá.
/// </summary>
public partial class Baogia
{
    public int MaBg { get; set; }

    public int MaYc { get; set; }

    public int MaPb { get; set; }

    public decimal TongGia { get; set; }

    public decimal KhoanCanThu { get; set; }

    public string DieuKienHuy { get; set; } = null!;

    public DateTime HanHieuLuc { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual Dondat? Dondat { get; set; }

    public virtual ICollection<Hangmucbaogia> Hangmucbaogia { get; set; } = new List<Hangmucbaogia>();

    public virtual Phienbanlichtrinh MaPbNavigation { get; set; } = null!;

    public virtual Yeucautourrieng MaYcNavigation { get; set; } = null!;
}
