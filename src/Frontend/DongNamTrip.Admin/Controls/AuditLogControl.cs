using DongNamTrip.Client;
using DongNamTrip.Contracts;
using static DongNamTrip.Admin.Scr19Ui;

namespace DongNamTrip.Admin;

public sealed class AuditLogControl : UserControl
{
    private readonly TripApiClient api;
    private readonly string token;
    private readonly Button previous = Ui.Button("Trang trước"), next = Ui.Button("Trang sau");
    private readonly ComboBox auditFilter = new() { Width = 165, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox search = new() { Width = 210, PlaceholderText = "Người thực hiện hoặc hành động" };
    private readonly DateTimePicker from = new() { Width = 150, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Checked = false };
    private readonly DateTimePicker to = new() { Width = 150, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Checked = false };
    private readonly Button auditRefresh = Ui.Button("Làm mới"), auditDetails = Ui.Button("Xem chi tiết");
    private readonly Label auditEmpty = new() { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.White, ForeColor = Ui.Muted, Visible = false, Dock = DockStyle.Fill };
    private List<AdminAuditRow> auditItems = [];
    private int page = 1;
    private bool more;
    private int auditVersion;
    private readonly DataGridView audit = Ui.Grid();
    private readonly Label auditInfo = Ui.Label("", 9);

    public AuditLogControl(TripApiClient api, string token)
    {
        this.api = api; this.token = token;
        var auditLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12), BackColor = Ui.Background };
        auditLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); auditLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); auditLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); auditLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        auditFilter.Items.AddRange(["Tất cả", "Nhân viên", "Khách hàng", "Hệ thống"]); auditFilter.SelectedIndex = 0;
        auditLayout.Controls.Add(Ui.Label("Lịch sử thao tác hệ thống", 17, true), 0, 0);
        auditLayout.Controls.Add(Bar(Ui.Label("Loại tác nhân"), auditFilter, Ui.Label("Từ ngày"), from, Ui.Label("Đến ngày"), to, search, auditRefresh, auditDetails), 0, 1);
        var auditContent = new Panel { Dock = DockStyle.Fill }; auditContent.Controls.Add(audit); auditContent.Controls.Add(auditEmpty); auditEmpty.BringToFront();
        auditLayout.Controls.Add(auditContent, 0, 2); auditLayout.Controls.Add(Bar(previous, next, auditInfo), 0, 3);
        Controls.Add(auditLayout);
        StyleGrid(audit);
        previous.Click += async (_, _) => { if (page > 1) { page--; await LoadAudit(); } };
        next.Click += async (_, _) => { if (more) { page++; await LoadAudit(); } };
        auditFilter.SelectedIndexChanged += async (_, _) => { page = 1; await LoadAudit(); };
        from.ValueChanged += async (_, _) => { page = 1; await LoadAudit(); };
        to.ValueChanged += async (_, _) => { page = 1; await LoadAudit(); };
        auditRefresh.Click += async (_, _) => { page = 1; await LoadAudit(); };
        search.TextChanged += (_, _) => { ++auditVersion; page = 1; previous.Enabled = next.Enabled = auditDetails.Enabled = false; auditRefresh.Enabled = true; auditItems = []; audit.DataSource = null; auditEmpty.Visible = false; auditInfo.Text = "Nhấn Enter hoặc Làm mới để tìm kiếm."; };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; page = 1; await LoadAudit(); } };
        audit.SelectionChanged += (_, _) => auditDetails.Enabled = audit.CurrentRow is not null && auditItems.Count > 0;
        auditDetails.Click += (_, _) => ShowAuditDetails();
        audit.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) ShowAuditDetails(); };
        Load += async (_, _) => await LoadAudit();
    }
    private async Task LoadAudit()
    {
        var version = ++auditVersion;
        var filter = auditFilter.SelectedIndex switch { 1 => "staff", 2 => "customer", 3 => "system", _ => "" };
        if (from.Checked && to.Checked && from.Value.Date > to.Value.Date) { auditItems = []; audit.DataSource = null; more = false; previous.Enabled = next.Enabled = auditDetails.Enabled = false; auditInfo.Text = "Từ ngày phải không sau đến ngày."; return; }
        previous.Enabled = next.Enabled = auditRefresh.Enabled = auditDetails.Enabled = false; auditEmpty.Visible = false; auditInfo.Text = "Đang tải…";
        auditItems = []; audit.DataSource = null;
        try
        {
            var result = await api.AdminAuditAsync(page, filter, from.Checked ? DateOnly.FromDateTime(from.Value) : null, to.Checked ? DateOnly.FromDateTime(to.Value) : null, search.Text.Trim(), token);
            if (version != auditVersion || IsDisposed) return;
            more = result.HasMore; auditItems = result.Items;
            audit.DataSource = result.Items.Select(x => new { Thời_gian = x.Time.AddHours(7).ToString("dd/MM/yyyy HH:mm"), Người_thực_hiện = x.Actor ?? "Không xác định", Loại_tác_nhân = ActorTypeLabel(x.ActorType), Hành_động = ActionLabel(x.Action), Đối_tượng = $"{x.ObjectType} {x.ObjectId}", Kết_quả = x.Result ?? "Không ghi nhận" }).ToList();
            foreach (DataGridViewColumn column in audit.Columns) column.HeaderText = column.HeaderText.Replace("_", " ");
            auditInfo.Text = $"Tổng {result.Total:N0} bản ghi · Trang {page}/{result.TotalPages}";
            auditEmpty.Text = filter == "customer" ? "Chưa có nguồn nhật ký nghiệp vụ khách hàng có loại tác nhân được ghi nhận." : "Không có nhật ký phù hợp.";
            auditEmpty.Visible = result.Items.Count == 0;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            if (version == auditVersion && !IsDisposed) { auditItems = []; audit.DataSource = null; more = false; auditInfo.Text = Kit.Describe(e); }
        }
        finally { if (version == auditVersion && !IsDisposed) { previous.Enabled = page > 1; next.Enabled = more; auditRefresh.Enabled = true; auditDetails.Enabled = auditItems.Count > 0; } }
    }
    private static string ActorTypeLabel(string type) => type switch { "staff" => "Nhân viên", "customer" => "Khách hàng", "system" => "Hệ thống", _ => "Không xác định" };
    private static string ActionLabel(string action) => action == "SeedCatalogPublicData" ? "Khởi tạo dữ liệu danh mục" : action;
    private void ShowAuditDetails()
    {
        if (audit.CurrentRow is not { Index: >= 0 } row || row.Index >= auditItems.Count) return;
        var entry = auditItems[row.Index];
        using var dialog = new Form { Text = "Chi tiết nhật ký", StartPosition = FormStartPosition.CenterParent, Size = new Size(650, 500), MinimumSize = new Size(400, 300), Font = Font };
        var lines = new List<string> { $"Thời điểm (UTC+7): {entry.Time.AddHours(7):dd/MM/yyyy HH:mm:ss}", $"Người thực hiện: {entry.Actor ?? "Không xác định"}", $"Loại tác nhân: {ActorTypeLabel(entry.ActorType)}", $"Hành động: {ActionLabel(entry.Action)}", $"Mã hành động: {entry.Action}", $"Đối tượng: {entry.ObjectType}", $"Mã bản ghi liên quan: {entry.ObjectId ?? "Không ghi nhận"}", $"Kết quả: {entry.Result ?? "Không ghi nhận"}" };
        if (!string.IsNullOrWhiteSpace(entry.Reason)) lines.Add($"Lý do: {entry.Reason}");
        if (!string.IsNullOrWhiteSpace(entry.Before)) lines.Add($"\r\nDữ liệu trước:\r\n{FormatData(entry.Before)}");
        if (!string.IsNullOrWhiteSpace(entry.After)) lines.Add($"\r\nDữ liệu sau:\r\n{FormatData(entry.After)}");
        var text = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = true, Text = string.Join("\r\n", lines) };
        dialog.Controls.Add(text); dialog.ShowDialog(this);
    }
    private static string FormatData(string value)
    {
        try { using var json = System.Text.Json.JsonDocument.Parse(value); return System.Text.Json.JsonSerializer.Serialize(json.RootElement, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }); }
        catch (System.Text.Json.JsonException) { return value; }
    }

}
