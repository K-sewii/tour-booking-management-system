using System;
using System.Collections.Generic;

namespace DongNamTrip.Api.Data.Entities;

/// <summary>
/// Lưu người thực hiện, thời điểm, đối tượng, lý do và dữ liệu thay đổi để đáp ứng UC24 và BR13.
/// </summary>
public partial class Nhatkythaotac
{
    public long MaNk { get; set; }

    public int? MaTk { get; set; }

    public DateTime ThoiDiem { get; set; }

    public string HanhDong { get; set; } = null!;

    public string LoaiDoiTuong { get; set; } = null!;

    public string? MaDoiTuong { get; set; }

    public string? LyDo { get; set; }

    public string? DuLieuTruoc { get; set; }

    public string? DuLieuSau { get; set; }

    public string? DiaChiIp { get; set; }

    public virtual Taikhoan? MaTkNavigation { get; set; }
}
