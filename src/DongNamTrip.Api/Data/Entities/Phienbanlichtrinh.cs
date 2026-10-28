using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu bản chụp lịch trình tại thời điểm gửi yêu cầu hoặc chốt phương án.
/// </summary>
public partial class Phienbanlichtrinh
{
    public int MaPb { get; set; }

    public int MaLt { get; set; }

    public int SoPhienBan { get; set; }

    public DateTime ThoiDiemTao { get; set; }

    public string NoiDungJson { get; set; } = null!;

    public virtual ICollection<Baogia> Baogia { get; set; } = new List<Baogia>();

    public virtual Lichtrinh MaLtNavigation { get; set; } = null!;

    public virtual ICollection<Yeucautourrieng> Yeucautourrieng { get; set; } = new List<Yeucautourrieng>();
}
