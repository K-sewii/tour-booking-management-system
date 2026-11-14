using System.ComponentModel.DataAnnotations;
using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

public sealed class LoginForm : Form
{
    private const string InvalidCredentialsMessage = "Không thể đăng nhập. Vui lòng kiểm tra email hoặc mật khẩu.";

    public LoginResponse? Session { get; private set; }

    public LoginForm(TripApiClient api)
    {
        Text = "DongNamTrip - Đăng nhập";
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(500, 520);
        MinimumSize = new Size(500, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        AutoScroll = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);
        BackColor = Ui.Background;

        var content = new TableLayoutPanel
        {
            AutoSize = false,
            BackColor = Color.White,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(32, 24, 32, 22),
            RowCount = 7
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Margin = Padding.Empty,
            RowCount = 2
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var brand = Ui.Label("DongNamTrip", 20F, true);
        brand.Margin = Padding.Empty;
        var caption = Ui.Label("KHÔNG GIAN QUẢN TRỊ", 9F);
        caption.ForeColor = Ui.Muted;
        caption.Margin = new Padding(0, 2, 0, 0);
        header.Controls.Add(brand, 0, 0);
        header.Controls.Add(caption, 0, 1);

        var fields = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Margin = Padding.Empty,
            RowCount = 6
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (var index = 0; index < fields.RowCount; index++)
            fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var emailLabel = Ui.Label("Email nhân viên *", 10F);
        emailLabel.Margin = new Padding(0, 0, 0, 4);
        emailLabel.TabStop = false;
        var email = CreateTextBox("Email nhân viên", "name@example.com", 0);
        var emailError = CreateErrorLabel();

        var passwordLabel = Ui.Label("Mật khẩu *", 10F);
        passwordLabel.Margin = new Padding(0, 14, 0, 4);
        passwordLabel.TabStop = false;
        var password = CreateTextBox("Mật khẩu", null, 1);
        password.MaxLength = Limits.PasswordMax;
        password.UseSystemPasswordChar = true;
        var passwordError = CreateErrorLabel();

        fields.Controls.Add(emailLabel, 0, 0);
        fields.Controls.Add(email, 0, 1);
        fields.Controls.Add(emailError, 0, 2);
        fields.Controls.Add(passwordLabel, 0, 3);
        fields.Controls.Add(password, 0, 4);
        fields.Controls.Add(passwordError, 0, 5);

        var formError = CreateErrorLabel();
        formError.Margin = new Padding(0, 12, 0, 0);

        var login = Ui.Button("Đăng nhập", true);
        login.AutoSize = false;
        login.Dock = DockStyle.Top;
        login.Height = 48;
        login.MinimumSize = new Size(0, 48);
        login.Padding = new Padding(12, 8, 12, 8);
        login.TextAlign = ContentAlignment.MiddleCenter;
        login.Margin = new Padding(0, 16, 0, 0);
        login.TabIndex = 2;

        var footer = Ui.Label("Chỉ tài khoản được phân công ít nhất một vai trò nhân viên mới có thể mở ứng dụng này.", 9F, wrap: true);
        footer.ForeColor = Ui.Muted;
        footer.Dock = DockStyle.Bottom;
        footer.Margin = Padding.Empty;

        content.Controls.Add(header, 0, 0);
        content.Controls.Add(fields, 0, 2);
        content.Controls.Add(formError, 0, 3);
        content.Controls.Add(login, 0, 4);
        content.Controls.Add(footer, 0, 6);
        Controls.Add(content);

        AcceptButton = login;
        Shown += (_, _) => email.Focus();
        email.TextChanged += (_, _) => ClearError(emailError);
        password.TextChanged += (_, _) => ClearError(passwordError);
        login.Click += async (_, _) => await SubmitAsync();

        async Task SubmitAsync()
        {
            var normalizedEmail = email.Text.Trim();
            if (!IsValid(normalizedEmail, password.Text, out var emailMessage, out var passwordMessage))
            {
                ClearError(formError);
                SetError(emailError, emailMessage);
                SetError(passwordError, passwordMessage);
                (emailMessage is not null ? email : password).Focus();
                return;
            }

            ClearError(emailError);
            ClearError(passwordError);
            ClearError(formError);
            SetBusy(true);
            try
            {
                var session = await api.LoginAsync(new LoginRequest { Email = normalizedEmail, Password = password.Text });
                if (!AppRoles.HasAny(session.User.Roles, AppRoles.Staff))
                {
                    password.Clear();
                    try
                    {
                        await api.LogoutAsync(session.Token);
                    }
                    catch (HttpRequestException)
                    {
                        // The access decision is already final; logout is best-effort.
                    }
                    catch (TaskCanceledException)
                    {
                        // The access decision is already final; logout is best-effort.
                    }

                    ShowFormError("Cần tài khoản nhân viên để mở ứng dụng quản trị.");
                    return;
                }

                Session = session;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (HttpRequestException exception)
            {
                password.Clear();
                ShowFormError(LoginErrorMessage(exception));
            }
            catch (TaskCanceledException)
            {
                password.Clear();
                ShowFormError("API phản hồi chậm. Vui lòng thử lại.");
            }
            finally
            {
                if (!IsDisposed)
                    SetBusy(false);
            }
        }

        void SetBusy(bool busy)
        {
            login.Enabled = !busy;
            login.Text = busy ? "Đang đăng nhập…" : "Đăng nhập";
            email.ReadOnly = busy;
            password.ReadOnly = busy;
            UseWaitCursor = busy;
        }

        void ShowFormError(string message)
        {
            SetError(formError, message);
        }
    }

    private static TextBox CreateTextBox(string accessibleName, string? placeholderText, int tabIndex) => new()
    {
        AccessibleName = accessibleName,
        AutoSize = false,
        Dock = DockStyle.Top,
        Height = 34,
        MaxLength = 254,
        PlaceholderText = placeholderText,
        TabIndex = tabIndex
    };

    private static Label CreateErrorLabel()
    {
        var error = Ui.Label(string.Empty, 9F, wrap: true);
        error.AccessibleRole = AccessibleRole.Alert;
        error.Dock = DockStyle.Top;
        error.ForeColor = Color.FromArgb(178, 34, 34);
        error.Margin = new Padding(0, 4, 0, 0);
        error.Visible = false;
        return error;
    }

    private static void SetError(Label error, string? message)
    {
        error.Text = message ?? string.Empty;
        error.Visible = !string.IsNullOrEmpty(message);
    }

    private static void ClearError(Label error) => SetError(error, null);

    private static bool IsValid(string email, string password, out string? emailMessage, out string? passwordMessage)
    {
        emailMessage = string.IsNullOrWhiteSpace(email)
            ? "Vui lòng nhập email."
            : email.Length > 254 || !new EmailAddressAttribute().IsValid(email)
                ? "Email không đúng định dạng hoặc vượt quá 254 ký tự."
                : null;

        passwordMessage = string.IsNullOrEmpty(password)
            ? "Vui lòng nhập mật khẩu."
            : password.Length > Limits.PasswordMax
                ? "Mật khẩu tối đa 128 ký tự."
                : null;

        return emailMessage is null && passwordMessage is null;
    }

    private static string LoginErrorMessage(HttpRequestException exception) => exception.StatusCode switch
    {
        null => "Không kết nối được API. Hãy kiểm tra DongNamTrip.Api đang chạy ở cổng 5080.",
        System.Net.HttpStatusCode.Unauthorized => InvalidCredentialsMessage,
        System.Net.HttpStatusCode.TooManyRequests => "Bạn đã thử đăng nhập quá nhiều lần. Vui lòng đợi ít phút rồi thử lại.",
        _ => "Không thể đăng nhập lúc này. Vui lòng thử lại sau."
    };
}
