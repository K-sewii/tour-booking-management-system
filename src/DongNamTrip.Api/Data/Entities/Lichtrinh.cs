using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu kế hoạch chuyến đi hiện tại và tài khoản sở hữu.
/// </summary>
public partial class Lichtrinh
{
    public int MaLt { get; set; }

    public int MaTk { get; set; }

    public string TenLt { get; set; } = null!;

    public DateOnly NgayDi { get; set; }

    public int SoKhach { get; set; }

    public string? NoiXuatPhat { get; set; }

    public string? PhuongTien { get; set; }

    public decimal? NganSach { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<LichtrinhDiadiem> LichtrinhDiadiem { get; set; } = new List<LichtrinhDiadiem>();

    public virtual ICollection<LichtrinhSuat> LichtrinhSuat { get; set; } = new List<LichtrinhSuat>();

    public virtual Taikhoan MaTkNavigation { get; set; } = null!;

    public virtual ICollection<Phienbanlichtrinh> Phienbanlichtrinh { get; set; } = new List<Phienbanlichtrinh>();
}
