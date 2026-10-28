using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu việc gán một hoặc nhiều vai trò cho tài khoản.
/// </summary>
public partial class TaikhoanVaitro
{
    public int MaTk { get; set; }

    public int MaVaiTro { get; set; }

    public DateTime NgayGan { get; set; }

    public virtual Taikhoan MaTkNavigation { get; set; } = null!;

    public virtual Vaitro MaVaiTroNavigation { get; set; } = null!;
}
