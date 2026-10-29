using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu nhu cầu tổ chức chuyến đi dựa trên một phiên bản lịch trình.
/// </summary>
public partial class Yeucautourrieng
{
    public int MaYc { get; set; }

    public int MaPb { get; set; }

    public int SoKhach { get; set; }

    public string LienHe { get; set; } = null!;

    public string? NhuCauDichVu { get; set; }

    public DateTime NgayGui { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<Baogia> Baogia { get; set; } = new List<Baogia>();

    public virtual Phienbanlichtrinh MaPbNavigation { get; set; } = null!;

    public virtual ICollection<Xacminhdichvu> Xacminhdichvu { get; set; } = new List<Xacminhdichvu>();
}
