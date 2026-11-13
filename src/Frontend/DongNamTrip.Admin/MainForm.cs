using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

public sealed class MainForm : Form
{
    public bool SessionEnded { get; private set; }

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

        var roles = session.User.Roles;
        bool Has(params string[] allowed) => AppRoles.HasAny(roles, allowed);

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var sidebarShell = new Panel { Dock = DockStyle.Fill, BackColor = Ui.Ink };
        var sidebar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Ui.Ink,
            Padding = new Padding(16, 20, 10, 10),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Margin = Padding.Empty
        };
        var brand = Ui.Label("DongNamTrip", 16, true);
        brand.ForeColor = Color.White;
        sidebar.Controls.Add(brand);
        var caption = Ui.Label("QUẢN TRỊ HÀNH TRÌNH", 9);
        caption.ForeColor = Color.FromArgb(178, 211, 190);
        caption.Margin = new Padding(0, 5, 0, 20);
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
        var account = Ui.Label($"{session.User.Name} · {string.Join(", ", roles)}", 10);
        account.Anchor = AnchorStyles.Right;
        top.Controls.Add(account);
        body.Controls.Add(top, 0, 0);
        body.Controls.Add(content, 0, 1);
        var status = Ui.Label("  Xác thực và phân quyền theo vai trò", 9);
        status.Dock = DockStyle.Fill;
        body.Controls.Add(status, 0, 2);
        sidebarShell.Controls.Add(sidebar);
        shell.Controls.Add(sidebarShell, 0, 0);
        shell.Controls.Add(body, 1, 0);
        Controls.Add(shell);

        Button? accountParent = null;
        var childButtons = new List<Button>();
        Button MakeNavButton(string title, bool child = false)
        {
            var button = Ui.Button(title);
            button.AutoSize = false;
            button.Width = 250;
            button.UseMnemonic = false;
            button.Font = new Font("Segoe UI", child ? 9 : 9.5f);
            button.Height = child ? 40 : 44;
            button.Padding = new Padding(child ? 27 : 12, 4, 6, 4);
            button.Margin = new Padding(0, 2, 0, 2);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.BackColor = Ui.Ink;
            button.ForeColor = Color.FromArgb(213, 232, 219);
            button.FlatAppearance.BorderSize = 0;
            sidebar.Controls.Add(button);
            return button;
        }
        void Nav(string title, Func<UserControl> factory, bool child = false)
        {
            var button = MakeNavButton(title, child);
            button.Click += (_, _) =>
            {
                foreach (var navButton in navButtons)
                    navButton.BackColor = Ui.Ink;
                if (accountParent is not null) accountParent.BackColor = child ? Color.FromArgb(31, 91, 79) : Ui.Ink;
                button.BackColor = Ui.Green;
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
            navButtons.Add(button);
            if (child) childButtons.Add(button);
        }

        Nav("Tổng quan", () => new DashboardControl(api, session.User.Name, session.Token, Has(AppRoles.Admin)));
        if (Has(AppRoles.Admin, AppRoles.Editor))
            Nav("Địa điểm", () => new DestinationsControl(api, session.Token));
        if (Has(AppRoles.Admin, AppRoles.Operator, AppRoles.Editor))
            Nav("Hoạt động & suất", () => new ActivitiesControl(api, session.Token));
        if (Has(AppRoles.Admin, AppRoles.Operator))
        {
            Nav("Tour & khởi hành", () => new ToursControl(api, session.Token));
            Nav("Đơn đặt tour", () => new BookingsControl(api, session.Token, handleOrders: true, handleMoney: false));
            Nav("Tour riêng & báo giá", () => new PrivateToursControl(api, session.Token));
        }
        // Admin có đủ quyền (kể cả Finance); Operator chỉ thấy khi được cấp thêm Finance. Đổi vai trò kết thúc phiên hiện tại.
        if (Has(AppRoles.Admin, AppRoles.Finance))
            Nav("Tài chính", () => new BookingsControl(api, session.Token, handleOrders: false, handleMoney: true));
        if (Has(AppRoles.Admin, AppRoles.Editor, AppRoles.Publisher))
            Nav("Thuyết minh & audio", () => new NarrationsControl(api, session.Token, session.User.Roles.ToArray()));
        if (Has(AppRoles.Admin))
        {
            accountParent = MakeNavButton("▾  Tài khoản và\n    phân quyền");
            accountParent.Height = 62;
            accountParent.Click += (_, _) =>
            {
                var expand = !childButtons[0].Visible;
                foreach (var child in childButtons) child.Visible = expand;
                accountParent.Text = (expand ? "▾  " : "▸  ") + "Tài khoản và\n    phân quyền";
            };
            Nav("Quản lý tài khoản", () => new AccountManagementControl(api, session.Token), true);
            Nav("Nhật ký hoạt động", () => new AuditLogControl(api, session.Token), true);
            Nav("Thống kê kinh doanh", () => new BusinessStatisticsControl(api, session.Token));
        }
        void FitSidebar()
        {
            var width = Math.Max(120, sidebar.ClientSize.Width - sidebar.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 4);
            foreach (var button in sidebar.Controls.OfType<Button>()) button.Width = width;
        }
        sidebar.Resize += (_, _) => FitSidebar();
        FitSidebar();

        var logout = Ui.Button("Đăng xuất & đóng");
        logout.Dock = DockStyle.Bottom;
        logout.AutoSize = false;
        logout.Height = 48;
        logout.UseMnemonic = false;
        sidebarShell.Controls.Add(logout);
        logout.Click += async (_, _) =>
        {
            var revoked = false;
            await Ui.RunAsync(this, async () =>
            {
                await api.LogoutAsync(session.Token);
                revoked = true;
            });
            if (revoked)
                EndSession();
        };
        Shown += (_, _) => navButtons[0].PerformClick();
    }

    internal void EndSession()
    {
        SessionEnded = true;
        Close();
    }
}
