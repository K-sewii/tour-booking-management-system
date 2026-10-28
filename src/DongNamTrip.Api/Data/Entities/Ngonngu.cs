using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu mã và tên ngôn ngữ được hỗ trợ cho bản dịch.
/// </summary>
public partial class Ngonngu
{
    public string MaNn { get; set; } = null!;

    public string TenNn { get; set; } = null!;

    public virtual ICollection<Bandich> Bandich { get; set; } = new List<Bandich>();
}
