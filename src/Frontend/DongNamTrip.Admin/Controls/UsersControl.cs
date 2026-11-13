using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

/// <summary>SCR19: người dùng và vai trò (UC24). Chỉ Admin.</summary>
public sealed class UsersControl : UserControl
{
    private readonly DataGridView grid = Ui.Grid();
    private readonly Label status = Ui.Label("", 10);
    private readonly TextBox search = new() { Width = 220, PlaceholderText = "Tên hoặc email" };
    private readonly ComboBox roleFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private List<AdminUser> users = [];

    public UsersControl(TripApiClient api, string token, int currentUserId)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 105));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        roleFilter.Items.Add("(Mọi vai trò)");
        roleFilter.Items.AddRange([.. AppRoles.Staff, AppRoles.Customer]);
        roleFilter.SelectedIndex = 0;
        var refresh = Ui.Button("Tải lại");
        var detail = Ui.Button("Chi tiết");
        var create = Ui.Button("Thêm nhân viên", true);
        var roles = Ui.Button("Đổi vai trò");
        var lockButton = Ui.Button("Khóa");
        var unlock = Ui.Button("Mở khóa");
        var reset = Ui.Button("Đặt lại mật khẩu");
        toolbar.Controls.AddRange([search, roleFilter, refresh, detail, create, roles, lockButton, unlock, reset]);
        layout.Controls.Add(toolbar, 0, 0); layout.Controls.Add(grid, 0, 1); layout.Controls.Add(status, 0, 2);
        Controls.Add(layout);

        async Task Reload()
        {
            var page = await api.AdminUsersAsync(new UserFilter(string.IsNullOrWhiteSpace(search.Text) ? null : search.Text.Trim(),
                roleFilter.SelectedIndex <= 0 ? null : roleFilter.Text, null, 1, 50), token);
            users = page.Items.ToList();
            if (IsDisposed) return;
            grid.DataSource = users.Select(x => new { Mã = x.Id, Họ_tên = x.FullName, Email = x.Email, Vai_trò = string.Join(", ", x.Roles), Trạng_thái = AccountStatus.Label(x.Status), Ngày_tạo = x.CreatedAt }).ToList();
            status.Text = $"{page.Total} tài khoản. Đổi vai trò hoặc khóa buộc người dùng đăng nhập lại.";
        }
        AdminUser? Current() => grid.CurrentRow is null ? null : users.FirstOrDefault(x => x.Id == Convert.ToInt32(grid.CurrentRow.Cells["Mã"].Value));
        Task Act(Func<AdminUser, Task> action) => Current() is { } user ? Ui.RunAsync(this, async () => { await action(user); await Reload(); }) : Task.CompletedTask;

        refresh.Click += async (_, _) => await Ui.RunAsync(this, Reload);
        roleFilter.SelectedIndexChanged += async (_, _) => await Ui.RunAsync(this, Reload);
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) await Ui.RunAsync(this, Reload); };
        detail.Click += async (_, _) =>
        {
            if (Current() is not { } user) return;
            await Ui.RunAsync(this, async () =>
            {
                var d = await api.AdminUserAsync(user.Id, token);
                var recent = string.Join(Environment.NewLine, d.RecentActions.Select(x => $"{x.At:dd/MM/yyyy HH:mm} · {x.Action} · {x.ObjectType} {x.ObjectId}"));
                MessageBox.Show(this, $"{d.User.FullName} <{d.User.Email}>\nSĐT: {d.User.Phone}\nVai trò: {string.Join(", ", d.User.Roles)}\nTrạng thái: {AccountStatus.Label(d.User.Status)}\nPhiên đăng nhập đang mở: {d.ActiveSessions}\n\nThao tác gần đây:\n{recent}", "Chi tiết tài khoản");
            });
        };
        create.Click += async (_, _) => await Ui.RunAsync(this, async () =>
        {
            using var dialog = new FieldsDialog("Thêm nhân viên",
            [
                new("Email", ""), new("Họ tên", ""), new("Số điện thoại (không bắt buộc)", ""), new($"Vai trò (chọn một; đổi thêm ở \"Đổi vai trò\")", AppRoles.Operator, Options: [.. AppRoles.Staff])
            ], v => v[0].Length == 0 || v[1].Length == 0 ? "Nhập email và họ tên." : null);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            var v = dialog.Values;
            var created = await api.CreateStaffAsync(new StaffForm(v[0], v[1], v[2].Length == 0 ? null : v[2], [v[3]]), token);
            MessageBox.Show(this, $"Đã tạo tài khoản {created.User.Email}.\nMật khẩu tạm (chỉ hiển thị một lần): {created.TemporaryPassword}", "Tài khoản nhân viên");
            await Reload();
        });
        roles.Click += async (_, _) => await Act(async user =>
        {
            using var dialog = new RolesDialog(user);
            if (dialog.ShowDialog(this) == DialogResult.OK) await api.SetUserRolesAsync(user.Id, dialog.Value, token);
        });
        lockButton.Click += async (_, _) => await Act(async user =>
        {
            if (user.Id == currentUserId) { Kit.Warn(this, "Bạn không thể tự khóa tài khoản của chính mình."); return; }
            using var dialog = new ReasonDialog($"Khóa tài khoản {user.Email}", "Lý do khóa");
            if (dialog.ShowDialog(this) == DialogResult.OK) await api.SetUserStatusAsync(user.Id, AccountStatus.Locked, dialog.Reason, token);
        });
        unlock.Click += async (_, _) => await Act(user => api.SetUserStatusAsync(user.Id, AccountStatus.Active, null, token));
        reset.Click += async (_, _) => await Act(async user =>
        {
            if (!Kit.Confirm(this, $"Đặt lại mật khẩu cho {user.Email}? Mọi phiên đăng nhập của tài khoản sẽ bị đóng.")) return;
            var temp = await api.ResetUserPasswordAsync(user.Id, token);
            MessageBox.Show(this, $"Mật khẩu tạm (chỉ hiển thị một lần): {temp.Password}", "Đặt lại mật khẩu");
        });
        Load += async (_, _) => await Ui.RunAsync(this, Reload);
    }
}

internal sealed class RolesDialog : Form
{
    private readonly CheckedListBox list = new() { Dock = DockStyle.Fill, CheckOnClick = true };

    public RolesDialog(AdminUser user)
    {
        Text = "Vai trò · " + user.Email; Size = new Size(380, 380); StartPosition = FormStartPosition.CenterParent;
        foreach (var role in AppRoles.Staff.Append(AppRoles.Customer)) list.Items.Add(role, user.Roles.Contains(role));
        var save = Ui.Button("Lưu vai trò", true);
        save.Click += (_, _) =>
        {
            if (list.CheckedItems.Count == 0) { MessageBox.Show(this, "Chọn ít nhất một vai trò."); return; }
            DialogResult = DialogResult.OK;
        };
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56 };
        bottom.Controls.Add(save);
        Controls.Add(list); Controls.Add(bottom);
    }

    public IReadOnlyList<string> Value => list.CheckedItems.Cast<string>().ToList();
}
