using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu nội dung gốc và phiên bản thuyết minh của địa điểm.
/// </summary>
public partial class Thuyetminh
{
    public int MaTm { get; set; }

    public int MaDd { get; set; }

    public string TieuDe { get; set; } = null!;

    public string NoiDungGoc { get; set; } = null!;

    public int PhienBan { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<Bandich> Bandich { get; set; } = new List<Bandich>();

    public virtual Diadiem MaDdNavigation { get; set; } = null!;
}
