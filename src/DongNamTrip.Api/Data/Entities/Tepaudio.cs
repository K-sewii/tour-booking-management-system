using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu tệp giọng đọc gắn với bản dịch và trạng thái công bố.
/// </summary>
public partial class Tepaudio
{
    public int MaTep { get; set; }

    public int MaBd { get; set; }

    public string GiongDoc { get; set; } = null!;

    public string DuongDan { get; set; } = null!;

    public string TrangThai { get; set; } = null!;

    public DateTime? NgayCongBo { get; set; }

    public virtual Bandich MaBdNavigation { get; set; } = null!;
}
