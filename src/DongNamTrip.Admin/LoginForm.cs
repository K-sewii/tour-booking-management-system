using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;
public sealed class LoginForm : Form
{
    public LoginResponse? Session { get; private set; }

    public LoginForm(TripApiClient api)
    {
        Text = "DongNamTrip · Đăng nhập quản trị";
        ClientSize = new Size(455, 475);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BackColor = Ui.Background;
        AutoScaleMode = AutoScaleMode.Dpi;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(34),
            ColumnCount = 1,
            RowCount = 9
        };
        var email = new TextBox
        {
            Text = "admin@dongnamtrip.local",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 3, 0, 12)
        };
        var password = new TextBox
        {
            UseSystemPasswordChar = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 3, 0, 12)
        };
        var login = Ui.Button("Đăng nhập", true);
        login.Dock = DockStyle.Fill;
        layout.Controls.Add(Ui.Label("DongNamTrip", 24, true));
        layout.Controls.Add(Ui.Label("KHÔNG GIAN QUẢN TRỊ · BẢN DEMO", 9));
        layout.Controls.Add(Ui.Label("Email quản trị"));
        layout.Controls.Add(email);
        layout.Controls.Add(Ui.Label("Mật khẩu"));
        layout.Controls.Add(password);
        layout.Controls.Add(login);
        layout.Controls.Add(Ui.Label("Mật khẩu thử nghiệm: Demo@123", 10));
        layout.Controls.Add(Ui.Label("Chạy API trước khi đăng nhập.\nDữ liệu mẫu sẽ mất khi API khởi động lại.", 9));
        Controls.Add(layout);
        AcceptButton = login;
        login.Click += async (_, _) => await Ui.RunAsync(this, async () =>
        {
            var session = await api.LoginAsync(new LoginRequest { Email = email.Text, Password = password.Text });
            if (session.User.Role != "Admin")
            {
                await api.LogoutAsync(session.Token);
                MessageBox.Show(this, "Cần tài khoản quản trị để mở WinForms.");
                return;
            }

            Session = session;
            DialogResult = DialogResult.OK;
            Close();
        });
    }
}
