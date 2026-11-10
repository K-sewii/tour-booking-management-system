using System.Globalization;
using System.Text.RegularExpressions;
namespace DongNamTrip.Admin;

/// <summary>Hàm hỗ trợ dùng chung cho các màn hình quản trị của Vương (SCR14, SCR15, SCR20).</summary>
internal static class Kit
{
    public static string Status(string code) => DongNamTrip.Contracts.ContentStatus.Label(code);

    public static string Describe(Exception error) => error switch
    {
        HttpRequestException { StatusCode: null } => "Không kết nối được API. Hãy chạy DongNamTrip.Api ở cổng 5080.",
        HttpRequestException http => http.Message,
        TaskCanceledException => "API phản hồi chậm. Vui lòng thử lại.",
        _ => error.Message
    };

    public static void Warn(IWin32Window owner, string message, string title = "Chưa thực hiện được") =>
        MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static bool Confirm(IWin32Window owner, string message, string title = "Xác nhận") =>
        MessageBox.Show(owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    public static int? SelectedId(DataGridView grid) =>
        grid.CurrentRow is { } row && grid.Columns.Contains("Id") && row.Cells["Id"].Value is { } value ? Convert.ToInt32(value) : null;

    public static void SelectRow(DataGridView grid, int? id)
    {
        if (id is null || !grid.Columns.Contains("Id")) return;
        foreach (DataGridViewRow row in grid.Rows)
            if (row.Cells["Id"].Value is { } value && Convert.ToInt32(value) == id)
            {
                row.Selected = true;
                grid.CurrentCell = row.Cells[0];
                return;
            }
    }

    public static decimal? ParseDecimal(string text)
    {
        var clean = text.Trim().Replace(',', '.');
        if (clean.Length == 0) return null;
        return decimal.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : throw new FormatException(clean);
    }

    /// <summary>Số tiền VND nguyên: nhận 150000, 150.000, 150,000 hoặc 150 000. Từ chối số lẻ để "150.000" không bị lưu thành 150.</summary>
    public static decimal? ParseMoney(string text)
    {
        var clean = text.Trim();
        if (clean.Length == 0) return null;
        if (Regex.IsMatch(clean, "^[0-9]+$") || Regex.IsMatch(clean, @"^[0-9]{1,3}([.,\s][0-9]{3})+$"))
            return decimal.Parse(Regex.Replace(clean, @"[.,\s]", ""), CultureInfo.InvariantCulture);
        throw new FormatException(clean);
    }

    public static string Money(decimal? value) => value is null ? "—" : value == 0 ? "Miễn phí" : value.Value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
}
