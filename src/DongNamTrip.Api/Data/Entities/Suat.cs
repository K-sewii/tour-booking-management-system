using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu ngày giờ tổ chức và trạng thái công bố của hoạt động.
/// </summary>
public partial class Suat
{
    public int MaSuat { get; set; }

    public int MaHd { get; set; }

    public DateOnly Ngay { get; set; }

    public TimeOnly GioBatDau { get; set; }

    public TimeOnly GioKetThuc { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<LichtrinhSuat> LichtrinhSuat { get; set; } = new List<LichtrinhSuat>();

    public virtual Hoatdong MaHdNavigation { get; set; } = null!;
}
