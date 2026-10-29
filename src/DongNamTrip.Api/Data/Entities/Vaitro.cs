using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu danh mục vai trò và ý nghĩa của từng vai trò.
/// </summary>
public partial class Vaitro
{
    public int MaVaiTro { get; set; }

    public string TenVaiTro { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<TaikhoanVaitro> TaikhoanVaitro { get; set; } = new List<TaikhoanVaitro>();
}
