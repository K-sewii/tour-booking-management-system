using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu từng dịch vụ, số lượng và đơn giá cấu thành báo giá.
/// </summary>
public partial class Hangmucbaogia
{
    public int MaHm { get; set; }

    public int MaBg { get; set; }

    public string MoTa { get; set; } = null!;

    public string DonViTinh { get; set; } = null!;

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public virtual Baogia MaBgNavigation { get; set; } = null!;
}
