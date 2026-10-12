namespace DongNamTrip.Admin;
internal static class Ui
{
    public static readonly Color Green = Color.FromArgb(8, 127, 108), Ink = Color.FromArgb(20, 59, 53), Muted = Color.FromArgb(98, 115, 111), Background = Color.FromArgb(246, 248, 244);
    public static Label Label(string text, float size = 10, bool bold = false) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Ink,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        Margin = new Padding(0, 8, 0, 8)
    };
    public static Button Button(string text, bool primary = false) => new()
    {
        Text = text,
        AutoSize = true,
        Height = 38,
        Padding = new Padding(12, 6, 12, 6),
        BackColor = primary ? Green : Color.White,
        ForeColor = primary ? Color.White : Ink,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 5, 10, 5)
    };
    public static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        RowHeadersVisible = false,
        EnableHeadersVisualStyles = false,
        ColumnHeadersHeight = 42,
        RowTemplate =
        {
            Height = 40
        },
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(229, 239, 231),
            ForeColor = Ink,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            Font = new Font("Segoe UI", 10),
            ForeColor = Ink,
            SelectionBackColor = Color.FromArgb(213, 237, 222),
            SelectionForeColor = Ink,
            Padding = new Padding(7)
        },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(249, 251, 248)
        }
    };
    public static async Task RunAsync(Control owner, Func<Task> action)
    {
        owner.Enabled = false;
        try
        {
            await action();
        }
        catch (HttpRequestException e)
        {
            if (!owner.IsDisposed) MessageBox.Show(owner, e.StatusCode is null ? "Không kết nối được API. Hãy chạy DongNamTrip.Api ở cổng 5080." : e.Message, "Chưa thực hiện được", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (TaskCanceledException)
        {
            if (!owner.IsDisposed) MessageBox.Show(owner, "API phản hồi chậm. Vui lòng thử lại.", "Kết nối");
        }
        finally
        {
            if (!owner.IsDisposed)
                owner.Enabled = true;
        }
    }
}
