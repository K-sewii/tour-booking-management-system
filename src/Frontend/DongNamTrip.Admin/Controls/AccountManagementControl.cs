using DongNamTrip.Client;
using DongNamTrip.Contracts;
using static DongNamTrip.Admin.Scr19Ui;

namespace DongNamTrip.Admin;

public sealed class AccountManagementControl : UserControl
{
    private readonly TripApiClient api;
    private readonly string token;
    private readonly TextBox search = new() { Width = 220, PlaceholderText = "Tìm họ tên hoặc email" };
    private readonly ComboBox group = new() { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox accountStatus = new() { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox role = new() { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label[] accountKpis = [Ui.Label("—", 15, true), Ui.Label("—", 15, true), Ui.Label("—", 15, true)];
    private readonly Button refresh = Ui.Button("Làm mới"), edit = Ui.Button("Gán / thu hồi vai trò", true), status = Ui.Button("Khóa / Mở khóa");
    private readonly Button details = Ui.Button("Xem thông tin"), promote = Ui.Button("Cấp quyền nhân viên");
    private readonly Button create = Ui.Button("Thêm nhân viên"), reset = Ui.Button("Đặt lại mật khẩu");
    private readonly Button accountPrevious = Ui.Button("Trang trước"), accountNext = Ui.Button("Trang sau");
    private readonly Label accountPageInfo = Ui.Label("", 9);
    private int accountPage = 1, accountPages = 1;
    private List<AdminAccount> items = [];
    private List<string> roles = [];
    private bool loadingAccounts;
    private int requestVersion;
    private readonly DataGridView accounts = Ui.Grid();
    private readonly Label accountTitle = Ui.Label("Quản lý nhân viên", 17, true), info = Ui.Label("Đang tải…", 9);

    public AccountManagementControl(TripApiClient api, string token)
    {
        this.api = api; this.token = token;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(12), BackColor = Ui.Background };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var title = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        title.Controls.Add(accountTitle); title.Controls.Add(Ui.Label("Tìm kiếm và quản lý quyền truy cập", 9));
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(Cards(accountKpis, "Tổng tài khoản", "Đang hoạt động", "Bị khóa"), 0, 1);
        group.Items.AddRange(["Nhân viên", "Khách hàng"]); group.SelectedIndex = 0;
        accountStatus.Items.AddRange(["Mọi trạng thái", "Hoạt động", "Bị khóa"]); accountStatus.SelectedIndex = 0;
        var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 2 };
        toolbar.Controls.Add(Bar(group, search, role, accountStatus, refresh), 0, 0);
        toolbar.Controls.Add(Bar(details, edit, promote, status, create, reset), 0, 1);
        layout.Controls.Add(toolbar, 0, 2);
        layout.Controls.Add(accounts, 0, 3); layout.Controls.Add(Bar(info, accountPrevious, accountPageInfo, accountNext), 0, 4);
        Controls.Add(layout);
        StyleGrid(accounts);
        refresh.Click += async (_, _) => await LoadAccounts();
        role.SelectedIndexChanged += async (_, _) => { accountPage = 1; await LoadAccounts(); };
        group.SelectedIndexChanged += async (_, _) => { accountPage = 1; role.Visible = group.SelectedIndex == 0; accountTitle.Text = group.SelectedIndex == 0 ? "Quản lý nhân viên" : "Quản lý khách hàng"; await LoadAccounts(); };
        accountStatus.SelectedIndexChanged += async (_, _) => { accountPage = 1; await LoadAccounts(); };
        accountPrevious.Click += async (_, _) => { if (accountPage > 1) { accountPage--; await LoadAccounts(); } };
        accountNext.Click += async (_, _) => { if (accountPage < accountPages) { accountPage++; await LoadAccounts(); } };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; accountPage = 1; await LoadAccounts(); } };
        accounts.SelectionChanged += (_, _) => UpdateButtons();
        edit.Click += async (_, _) => await EditRoles();
        promote.Click += async (_, _) => { if (Kit.Confirm(this, "Cấp quyền nhân viên cho khách hàng này? Hãy chọn vai trò trong hộp thoại tiếp theo.")) await EditRoles(); };
        details.Click += (_, _) => { if (Selected is { } selected) MessageBox.Show(this, $"Mã: {selected.Id}\nHọ tên: {selected.Name}\nEmail: {selected.Email}\nĐiện thoại: {selected.Phone ?? "—"}\nVai trò: {string.Join(", ", selected.Roles)}\nTrạng thái: {(selected.Status == "Active" ? "Hoạt động" : selected.Status == "Locked" ? "Bị khóa" : selected.Status)}", "Thông tin tài khoản"); };
        status.Click += async (_, _) => await ChangeStatus();
        create.Click += async (_, _) => await Ui.RunAsync(this, async () =>
        {
            using var dialog = new FieldsDialog("Thêm nhân viên",
                [new("Email", ""), new("Họ tên", ""), new("Số điện thoại (không bắt buộc)", ""), new("Vai trò", AppRoles.Operator, Options: [.. AppRoles.Staff])],
                v => v[0].Length == 0 || v[1].Length == 0 ? "Nhập email và họ tên." : null);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            var v = dialog.Values;
            var created = await api.CreateStaffAsync(new StaffForm(v[0], v[1], v[2].Length == 0 ? null : v[2], [v[3]]), token);
            MessageBox.Show(this, $"Đã tạo tài khoản {created.User.Email}.\nMật khẩu tạm (chỉ hiển thị một lần): {created.TemporaryPassword}", "Tài khoản nhân viên");
            await LoadAccounts();
        });
        reset.Click += async (_, _) => await Ui.RunAsync(this, async () =>
        {
            var selected = Selected; if (selected is null || !Kit.Confirm(this, $"Đặt lại mật khẩu cho {selected.Email}? Mọi phiên đăng nhập của tài khoản sẽ bị đóng.")) return;
            var temp = await api.ResetUserPasswordAsync(selected.Id, token);
            MessageBox.Show(this, $"Mật khẩu tạm (chỉ hiển thị một lần): {temp.Password}", "Đặt lại mật khẩu");
        });
        Load += async (_, _) => await Initialize();
    }
    private AdminAccount? Selected => accounts.CurrentRow?.DataBoundItem is AccountRow row ? items.FirstOrDefault(x => x.Id == row.Id) : null;
    private void UpdateButtons() { edit.Visible = group.SelectedIndex == 0; promote.Visible = group.SelectedIndex == 1; edit.Enabled = promote.Enabled = details.Enabled = status.Enabled = reset.Enabled = !loadingAccounts && Selected is not null; }
    private async Task Initialize()
    {
        try { roles = await api.AdminRolesAsync(token); if (IsDisposed) return; role.Items.Add("Mọi vai trò"); role.Items.AddRange(roles.Cast<object>().ToArray()); role.SelectedIndex = 0; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { if (!Kit.HandleExpiredSession(this, e) && !IsDisposed) info.Text = Kit.Describe(e); }
    }
    private async Task LoadAccounts()
    {
        var version = ++requestVersion;
        loadingAccounts = true; accounts.Enabled = false; accounts.DataSource = null; items = []; foreach (var value in accountKpis) value.Text = "Đang tải…"; accountPageInfo.Text = ""; accountPrevious.Enabled = accountNext.Enabled = false; UpdateButtons();
        refresh.Enabled = edit.Enabled = promote.Enabled = details.Enabled = status.Enabled = false; info.Text = "Đang tải…";
        try
        {
            var result = await api.AdminAccountPageAsync(group.SelectedIndex == 0 ? "staff" : "customer", search.Text.Trim(), group.SelectedIndex == 0 && role.SelectedIndex > 0 ? role.Text : "", accountStatus.SelectedIndex == 1 ? "Active" : accountStatus.SelectedIndex == 2 ? "Locked" : "", accountPage, token);
            if (version != requestVersion || IsDisposed) return;
            items = result.Items; accountPage = result.Page; accountPages = result.TotalPages;
            accountKpis[0].Text = result.Total.ToString("N0"); accountKpis[1].Text = result.Active.ToString("N0"); accountKpis[2].Text = result.Locked.ToString("N0");
            accounts.DataSource = items.Select(x => new AccountRow(x.Id, x.Name, x.Email, string.Join(", ", x.Roles), x.Phone ?? "—", x.Status == "Active" ? "Hoạt động" : x.Status == "Locked" ? "Bị khóa" : x.Status)).ToList();
            accounts.Columns[nameof(AccountRow.Id)]!.HeaderText = "Mã tài khoản";
            accounts.Columns[nameof(AccountRow.Họ_tên)]!.HeaderText = "Họ tên";
            accounts.Columns[nameof(AccountRow.Vai_trò)]!.HeaderText = "Vai trò"; accounts.Columns[nameof(AccountRow.Vai_trò)]!.Visible = group.SelectedIndex == 0;
            accounts.Columns[nameof(AccountRow.Phone)]!.HeaderText = "Số điện thoại"; accounts.Columns[nameof(AccountRow.Phone)]!.Visible = group.SelectedIndex == 1;
            accounts.Columns[nameof(AccountRow.Status)]!.HeaderText = "Trạng thái"; accounts.Columns[nameof(AccountRow.Email)]!.FillWeight = 150;
            info.Text = items.Count == 0 ? (search.Text.Trim().Length == 0 && (group.SelectedIndex == 1 || role.SelectedIndex <= 0) && accountStatus.SelectedIndex == 0 ? "Chưa có tài khoản trong nhóm này." : "Không có tài khoản phù hợp với bộ lọc.") : $"{items.Count} bản ghi trên trang";
            accountPageInfo.Text = $"Tổng {result.FilteredTotal:N0} bản ghi · Trang {accountPage}/{accountPages}";
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { if (!Kit.HandleExpiredSession(this, e) && version == requestVersion && !IsDisposed) { items = []; accounts.DataSource = null; accountPage = accountPages = 1; accountPageInfo.Text = ""; foreach (var value in accountKpis) value.Text = "Lỗi tải"; info.Text = e is HttpRequestException { StatusCode: System.Net.HttpStatusCode.NotFound } ? "API không có endpoint tài khoản phân trang. Hãy kiểm tra địa chỉ API và cập nhật phiên bản API." : Kit.Describe(e); } }
        finally { if (version == requestVersion && !IsDisposed) { loadingAccounts = false; accounts.Enabled = true; refresh.Enabled = true; accountPrevious.Enabled = accountPage > 1; accountNext.Enabled = accountPage < accountPages; UpdateButtons(); } }
    }
    private async Task ChangeStatus()
    {
        var selected = Selected; if (selected is null) return;
        var target = selected.Status == "Active" ? "Locked" : "Active";
        if (!Kit.Confirm(this, $"{(target == "Locked" ? "Khóa" : "Mở khóa")} tài khoản {selected.Name}?")) return;
        status.Enabled = edit.Enabled = false;
        try { await api.SetAdminStatusAsync(selected.Id, new(target, selected.Status), token); await LoadAccounts(); }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { if (!Kit.HandleExpiredSession(this, e)) { Kit.Warn(this, Kit.Describe(e)); UpdateButtons(); } }
    }
    private async Task EditRoles()
    {
        var selected = Selected; if (selected is null) return;
        using var dialog = new Form { Text = "Gán / thu hồi vai trò", StartPosition = FormStartPosition.CenterParent, Size = new Size(420, 420), MinimizeBox = false, MaximizeBox = false, Font = Font };
        var list = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
        foreach (var value in roles) list.Items.Add(value, selected.Roles.Contains(value));
        var save = Ui.Button("Lưu", true); var cancel = Ui.Button("Hủy"); cancel.DialogResult = DialogResult.Cancel;
        var buttons = Bar(save, cancel); buttons.Dock = DockStyle.Bottom;
        var accountLabel = Ui.Label($"Tài khoản: {selected.Name} · {selected.Email}", 10, true); accountLabel.Dock = DockStyle.Top;
        dialog.Controls.Add(list); dialog.Controls.Add(accountLabel); dialog.Controls.Add(buttons); dialog.AcceptButton = save; dialog.CancelButton = cancel;
        save.Click += async (_, _) =>
        {
            var chosen = list.CheckedItems.Cast<string>().ToArray();
            if (chosen.Length == 0) { Kit.Warn(dialog, "Tài khoản cần ít nhất một vai trò."); return; }
            if (group.SelectedIndex == 1 && !chosen.Any(x => x is AppRoles.Admin or AppRoles.Operator or AppRoles.Editor or AppRoles.Finance or AppRoles.Publisher)) { Kit.Warn(dialog, "Hãy chọn ít nhất một vai trò nhân viên."); return; }
            if (!Kit.Confirm(dialog, "Xác nhận cập nhật vai trò? Mọi phiên của tài khoản sẽ kết thúc và người dùng cần đăng nhập lại.")) return;
            save.Enabled = cancel.Enabled = false;
            try { await api.SetAdminRolesAsync(selected.Id, new(chosen, selected.Roles), token); dialog.DialogResult = DialogResult.OK; dialog.Close(); }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { if (!Kit.HandleExpiredSession(this, e)) { Kit.Warn(dialog, Kit.Describe(e)); save.Enabled = cancel.Enabled = true; } }
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) { MessageBox.Show(this, "Đã cập nhật vai trò. Các phiên của tài khoản đã kết thúc; người dùng cần đăng nhập lại.", "Thành công"); await LoadAccounts(); }
    }
    private sealed record AccountRow(int Id, string Họ_tên, string Email, string Vai_trò, string Phone, string Status);
}
