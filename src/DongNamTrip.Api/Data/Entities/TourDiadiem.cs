using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Liên kết tour với địa điểm, ngày thứ và thứ tự tham quan.
/// </summary>
public partial class TourDiadiem
{
    public int MaTour { get; set; }

    public int MaDd { get; set; }

    public int NgayThu { get; set; }

    public int ThuTu { get; set; }

    public virtual Diadiem MaDdNavigation { get; set; } = null!;

    public virtual Tour MaTourNavigation { get; set; } = null!;
}
