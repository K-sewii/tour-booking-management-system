namespace DongNamTrip.Admin;
internal static class Ui
{
    public static readonly Color Green = Color.FromArgb(8, 127, 108), Ink = Color.FromArgb(20, 59, 53), Muted = Color.FromArgb(98, 115, 111), Background = Color.FromArgb(246, 248, 244);
    public static Label Label(string text, float size = 10, bool bold = false, bool wrap = false)
    {
        var label = wrap ? new WrapLabel() : new Label();
        label.Text = text;
        label.AutoSize = true;
        label.ForeColor = Ink;
        label.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
        label.Margin = new Padding(0, 8, 0, 8);
        return label;
    }

    // Wraps text to the width its container offers instead of widening the container.
    private sealed class WrapLabel : Label
    {
        public override Size GetPreferredSize(Size proposedSize)
        {
            var width = proposedSize.Width is > 1 and < int.MaxValue / 2
                ? proposedSize.Width
                : Parent is null ? 0 : Parent.ClientSize.Width - Parent.Padding.Horizontal;
            return width > 0 ? base.GetPreferredSize(new Size(width - Margin.Horizontal, 0)) : base.GetPreferredSize(proposedSize);
        }
    }
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
            if (owner.IsDisposed)
                return;

            if (e.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                MessageBox.Show(owner, "Phiên đăng nhập đã hết hạn hoặc tài khoản không còn hoạt động. Vui lòng đăng nhập lại.", "Phiên đã kết thúc", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (owner.FindForm() is MainForm main)
                    main.EndSession();
                return;
            }

            MessageBox.Show(owner, e.StatusCode is null ? "Không kết nối được API. Hãy chạy DongNamTrip.Api ở cổng 5080." : e.Message, "Chưa thực hiện được", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
