using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu kết quả kiểm tra từng dịch vụ với đơn vị cung cấp trước khi báo giá.
/// </summary>
public partial class Xacminhdichvu
{
    public int MaXm { get; set; }

    public int MaYc { get; set; }

    public string HangMuc { get; set; } = null!;

    public string DonViCungCap { get; set; } = null!;

    public DateTime ThoiDiem { get; set; }

    public string KetQua { get; set; } = null!;

    public string? GhiChu { get; set; }

    public virtual Yeucautourrieng MaYcNavigation { get; set; } = null!;
}
