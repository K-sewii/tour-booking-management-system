using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu nội dung theo ngôn ngữ cho một phiên bản thuyết minh gốc.
/// </summary>
public partial class Bandich
{
    public int MaBd { get; set; }

    public int MaTm { get; set; }

    public string MaNn { get; set; } = null!;

    public string NoiDungDich { get; set; } = null!;

    public string TrangThai { get; set; } = null!;

    public virtual Ngonngu MaNnNavigation { get; set; } = null!;

    public virtual Thuyetminh MaTmNavigation { get; set; } = null!;

    public virtual ICollection<Tepaudio> Tepaudio { get; set; } = new List<Tepaudio>();
}
