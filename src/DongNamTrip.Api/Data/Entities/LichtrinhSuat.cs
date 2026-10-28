using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu các suất hoạt động khách mong muốn tham gia tại từng địa điểm.
/// </summary>
public partial class LichtrinhSuat
{
    public int MaLt { get; set; }

    public int MaSuat { get; set; }

    public int MaDd { get; set; }

    public string? GhiChu { get; set; }

    public virtual LichtrinhDiadiem LichtrinhDiadiem { get; set; } = null!;

    public virtual Diadiem MaDdNavigation { get; set; } = null!;

    public virtual Lichtrinh MaLtNavigation { get; set; } = null!;

    public virtual Suat MaSuatNavigation { get; set; } = null!;
}
