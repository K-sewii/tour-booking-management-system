using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu hoạt động tại địa điểm, điều kiện tham gia và phí tham khảo.
/// </summary>
public partial class Hoatdong
{
    public int MaHd { get; set; }

    public int MaDd { get; set; }

    public string TenHd { get; set; } = null!;

    public string? MoTa { get; set; }

    public int ThoiLuongPhut { get; set; }

    public string? DieuKien { get; set; }

    public decimal? PhiThamKhao { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual Diadiem MaDdNavigation { get; set; } = null!;

    public virtual ICollection<Suat> Suat { get; set; } = new List<Suat>();
}
