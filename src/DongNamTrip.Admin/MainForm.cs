using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;
public sealed class MainForm : Form
{
    private readonly Panel content = new()
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(28),
        BackColor = Ui.Background
    };
    private readonly Label heading = Ui.Label("Tổng quan", 22, true);
    private readonly List<Button> navButtons = [];
    public MainForm(TripApiClient api, LoginResponse session)
    {
        Text = "DongNamTrip · Quản trị";
        Size = new Size(1320, 820);
        MinimumSize = new Size(1060, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 245));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var sidebar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Ui.Ink,
            Padding = new Padding(20, 26, 15, 15),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Margin = Padding.Empty
        };
        var brand = Ui.Label("DongNamTrip", 21, true);
        brand.ForeColor = Color.White;
        sidebar.Controls.Add(brand);
        var caption = Ui.Label("QUẢN TRỊ HÀNH TRÌNH", 9);
        caption.ForeColor = Color.FromArgb(178, 211, 190);
        caption.Margin = new Padding(0, 5, 0, 35);
        sidebar.Controls.Add(caption);
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Margin = Padding.Empty
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(28, 20, 25, 10),
            BackColor = Color.White
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        top.Controls.Add(heading);
        var account = Ui.Label($"{session.User.Name} · Quản trị viên", 10);
        account.Anchor = AnchorStyles.Right;
        top.Controls.Add(account);
        body.Controls.Add(top, 0, 0);
        body.Controls.Add(content, 0, 1);
        var status = Ui.Label("  DEMO · Dữ liệu trong bộ nhớ · Chưa kết nối SQL Server", 9);
        status.Dock = DockStyle.Fill;
        body.Controls.Add(status, 0, 2);
        shell.Controls.Add(sidebar, 0, 0);
        shell.Controls.Add(body, 1, 0);
        Controls.Add(shell);
        void Nav(string title, Func<UserControl> factory)
        {
            var b = Ui.Button(title);
            b.AutoSize = false;
            b.Width = 200;
            b.Height = 46;
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.BackColor = Ui.Ink;
            b.ForeColor = Color.FromArgb(213, 232, 219);
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) =>
            {
                foreach (var button in navButtons)
                    button.BackColor = Ui.Ink;
                b.BackColor = Ui.Green;
                heading.Text = title;
                foreach (Control old in content.Controls.Cast<Control>().ToArray())
                {
                    content.Controls.Remove(old);
                    old.Dispose();
                }

                var screen = factory();
                screen.Dock = DockStyle.Fill;
                content.Controls.Add(screen);
            };
            navButtons.Add(b);
            sidebar.Controls.Add(b);
        }

        Nav("Tổng quan", () => new DashboardControl(api));
        Nav("Địa điểm", () => new DestinationsControl(api, session.Token));
        Nav("Hoạt động & suất", () => new ModuleControl("Hoạt động và suất tổ chức", "Vương", ["Hoạt động", "Địa điểm", "Ngày tổ chức", "Trạng thái"]));
        Nav("Tour & khởi hành", () => new ToursControl(api));
        Nav("Đơn đặt tour", () => new ModuleControl("Đơn đặt tour", "Kỳ", ["Mã đơn", "Khách hàng", "Tour", "Tổng tiền", "Trạng thái"]));
        Nav("Tour riêng & báo giá", () => new ModuleControl("Yêu cầu tour riêng và báo giá", "Nam", ["Mã yêu cầu", "Khách hàng", "Ngày dự kiến", "Số khách", "Trạng thái"]));
        Nav("Tài khoản", () => new ModuleControl("Tài khoản và phân quyền", "Đức", ["Họ tên", "Email", "Vai trò", "Trạng thái"]));
        var logout = Ui.Button("Đăng xuất & đóng");
        logout.Margin = new Padding(0, 30, 0, 0);
        sidebar.Controls.Add(logout);
        logout.Click += async (_, _) =>
        {
            await Ui.RunAsync(this, () => api.LogoutAsync(session.Token));
            Close();
        };
        Shown += (_, _) => navButtons[0].PerformClick();
    }
}
