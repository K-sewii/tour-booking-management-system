using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu từng đợt tổ chức tour, giá, sức chứa và số chỗ đã giữ.
/// </summary>
public partial class Lichkhoihanh
{
    public int MaLkh { get; set; }

    public int MaTour { get; set; }

    public DateTime NgayGioKhoiHanh { get; set; }

    public decimal Gia { get; set; }

    public int SucChua { get; set; }

    public int SoChoDaGiu { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<Dondat> Dondat { get; set; } = new List<Dondat>();

    public virtual Tour MaTourNavigation { get; set; } = null!;
}
