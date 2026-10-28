using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu các điểm dự kiến tham quan và thứ tự của lịch trình cá nhân.
/// </summary>
public partial class LichtrinhDiadiem
{
    public int MaLt { get; set; }

    public int MaDd { get; set; }

    public int NgayThu { get; set; }

    public int ThuTu { get; set; }

    public string? GhiChu { get; set; }

    public virtual ICollection<LichtrinhSuat> LichtrinhSuat { get; set; } = new List<LichtrinhSuat>();

    public virtual Diadiem MaDdNavigation { get; set; } = null!;

    public virtual Lichtrinh MaLtNavigation { get; set; } = null!;
}
