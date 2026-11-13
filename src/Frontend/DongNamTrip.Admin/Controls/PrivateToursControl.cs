using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

/// <summary>SCR18: yêu cầu tour riêng, xác minh dịch vụ, báo giá (UC17, UC18).</summary>
public sealed class PrivateToursControl : UserControl
{
    private readonly DataGridView grid = Ui.Grid();
    private readonly Label status = Ui.Label("");
    private readonly ComboBox statusFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    private List<QuoteRequestInfo> requests = [];

    public PrivateToursControl(TripApiClient api, string token)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var refresh = Ui.Button("Tải lại");
        var detail = Ui.Button("Xem yêu cầu");
        var receive = Ui.Button("Tiếp nhận");
        var verify = Ui.Button("Ghi xác minh");
        var quote = Ui.Button("Phát hành báo giá", true);
        var cancel = Ui.Button("Hủy yêu cầu");
        statusFilter.Items.Add("(Tất cả)");
        statusFilter.Items.AddRange(QuoteRequestStatus.All.Select(QuoteRequestStatus.Label).Cast<object>().ToArray());
        statusFilter.SelectedIndex = 0;
        actions.Controls.AddRange([statusFilter, refresh, detail, receive, verify, quote, cancel]);
        layout.Controls.Add(actions, 0, 0); layout.Controls.Add(grid, 0, 1); layout.Controls.Add(status, 0, 2); Controls.Add(layout);

        async Task Reload()
        {
            var filter = statusFilter.SelectedIndex <= 0 ? null : QuoteRequestStatus.All[statusFilter.SelectedIndex - 1];
            requests = (await api.AdminQuoteRequestsAsync(new QuoteRequestFilter(filter, null, null, 1, 50), token)).Items.ToList();
            if (IsDisposed) return;
            grid.DataSource = requests.Select(x => new { Mã = x.Id, Lịch_trình = x.ItineraryId, Phiên_bản = x.VersionNumber, Ngày_gửi = x.SentAt,
                Số_khách = x.Guests, Trạng_thái = QuoteRequestStatus.Label(x.Status), Báo_giá = x.CurrentQuoteId }).ToList();
            status.Text = $"{requests.Count} yêu cầu · Suất khách chọn chỉ là mong muốn, cần xác minh với nhà cung cấp.";
        }
        QuoteRequestInfo? Current() => grid.CurrentRow is null ? null : requests.FirstOrDefault(x => x.Id == Convert.ToInt32(grid.CurrentRow.Cells["Mã"].Value));

        refresh.Click += async (_, _) => await Ui.RunAsync(this, Reload);
        statusFilter.SelectedIndexChanged += async (_, _) => await Ui.RunAsync(this, Reload);
        detail.Click += async (_, _) =>
        {
            var current = Current(); if (current is null) return;
            await Ui.RunAsync(this, async () =>
            {
                var request = await api.AdminQuoteRequestAsync(current.Id, token);
                var warnings = string.Join(Environment.NewLine, request.Check.Warnings.Select(x => $"[{x.Code}] {x.Message}"));
                var checks = string.Join(Environment.NewLine, request.Verifications.Select(x => $"{x.Item}: {VerificationResults.Label(x.Result)} · {x.Provider} · {x.Note}"));
                var quotes = string.Join(Environment.NewLine, request.Quotes.Select(x => $"#{x.Id} {QuoteStatus.Label(x.Status)} · tổng {x.Total:N0} · cần thu {x.Deposit:N0} · hiệu lực đến {x.ValidUntil:dd/MM/yyyy HH:mm}"));
                var notes = string.Join(Environment.NewLine, request.ChangeNotes);
                MessageBox.Show(this, $"Yêu cầu #{request.Request.Id} · Phiên bản {request.Request.VersionNumber} · {QuoteRequestStatus.Label(request.Request.Status)}\nLiên hệ: {request.Contact}\nNhu cầu: {request.Needs}\nNgày đi: {request.Version.StartDate:dd/MM/yyyy} · {request.Version.Guests} khách\n\nKiểm tra lịch ({(request.Check.Fits ? "phù hợp" : "có cảnh báo")}{(request.Check.Complete ? "" : ", chưa đủ dữ liệu")}):\n{warnings}\n\nXác minh:\n{checks}\n\nBáo giá:\n{quotes}\n\nĐề nghị đổi của khách:\n{notes}", "Chi tiết tour riêng");
            });
        };
        receive.Click += async (_, _) =>
        {
            var current = Current(); if (current is null) return;
            await Ui.RunAsync(this, async () => { await api.ReceiveQuoteRequestAsync(current.Id, token); await Reload(); });
        };
        verify.Click += async (_, _) =>
        {
            var current = Current(); if (current is null) return;
            using var dialog = new VerificationDialog(); if (dialog.ShowDialog(this) != DialogResult.OK) return;
            await Ui.RunAsync(this, async () => { await api.RecordVerificationAsync(current.Id, dialog.Value, token); await Reload(); });
        };
        quote.Click += async (_, _) =>
        {
            var current = Current(); if (current is null) return;
            using var dialog = new QuoteDialog(); if (dialog.ShowDialog(this) != DialogResult.OK) return;
            await Ui.RunAsync(this, async () => { await api.IssueQuoteAsync(current.Id, dialog.Value!, token); await Reload(); });
        };
        cancel.Click += async (_, _) =>
        {
            var current = Current(); if (current is null) return;
            using var dialog = new ReasonDialog("Hủy yêu cầu tour riêng"); if (dialog.ShowDialog(this) != DialogResult.OK) return;
            await Ui.RunAsync(this, async () => { await api.CancelQuoteRequestByStaffAsync(current.Id, dialog.Reason, token); await Reload(); });
        };
        Load += async (_, _) => await Ui.RunAsync(this, Reload);
    }
}

internal sealed class VerificationDialog : Form
{
    private readonly TextBox item = new(), provider = new(), note = new() { Multiline = true, Height = 65 };
    private readonly ComboBox result = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    public VerificationForm Value => new(item.Text.Trim(), provider.Text.Trim(), DateTime.Now, VerificationResults.All[result.SelectedIndex], string.IsNullOrWhiteSpace(note.Text) ? null : note.Text.Trim());

    public VerificationDialog()
    {
        Text = "Xác minh dịch vụ"; Size = new Size(480, 420); StartPosition = FormStartPosition.CenterParent;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(20) };
        void Add(string label, Control control) { control.Width = 400; layout.Controls.Add(Ui.Label(label)); layout.Controls.Add(control); }
        Add("Hạng mục cần xác minh", item); Add("Đơn vị cung cấp", provider);
        result.Items.AddRange(VerificationResults.All.Select(VerificationResults.Label).Cast<object>().ToArray()); result.SelectedIndex = 0;
        Add("Kết quả", result); Add("Ghi chú (bắt buộc khi không đáp ứng hoặc thay thế: nêu phương án đề xuất)", note);
        var save = Ui.Button("Lưu xác minh", true); save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(item.Text) || string.IsNullOrWhiteSpace(provider.Text)) { MessageBox.Show(this, "Nhập hạng mục và đơn vị cung cấp."); return; }
            if (VerificationResults.All[result.SelectedIndex] != VerificationResults.Available && string.IsNullOrWhiteSpace(note.Text)) { MessageBox.Show(this, "Cần ghi chú đề xuất khi không đáp ứng hoặc thay thế."); return; }
            DialogResult = DialogResult.OK;
        };
        layout.Controls.Add(save); Controls.Add(layout);
    }
}

internal sealed class QuoteDialog : Form
{
    private readonly DateTimePicker expiry = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Value = DateTime.Now.AddDays(7) };
    private readonly NumericUpDown deposit = new() { Maximum = 999999999999999999, ThousandsSeparator = true };
    private readonly TextBox terms = new() { Multiline = true, Height = 55 };
    private readonly DataGridView lines = new() { AllowUserToAddRows = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Height = 250 };
    public QuoteForm? Value { get; private set; }

    public QuoteDialog()
    {
        Text = "Phát hành báo giá"; Size = new Size(780, 640); StartPosition = FormStartPosition.CenterParent;
        lines.Columns.Add("Description", "Hạng mục"); lines.Columns.Add("Unit", "Đơn vị"); lines.Columns.Add("Quantity", "Số lượng"); lines.Columns.Add("UnitPrice", "Đơn giá (VNĐ)");
        lines.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IncludedInPackage", HeaderText = "Đã trong gói" });
        lines.Rows.Add("Dịch vụ tour riêng", "gói", "1", "0", false);
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(20) };
        layout.Controls.Add(Ui.Label("Hạng mục (mục đã nằm trong giá trọn gói được hiển thị nhưng không cộng lần hai)")); lines.Width = 700; layout.Controls.Add(lines);
        void Add(string label, Control control) { control.Width = 700; layout.Controls.Add(Ui.Label(label)); layout.Controls.Add(control); }
        Add("Hạn hiệu lực", expiry); Add("Khoản cần thu (VNĐ)", deposit); Add("Điều kiện hủy", terms);
        var save = Ui.Button("Phát hành", true); save.Click += (_, _) =>
        {
            lines.EndEdit();
            var items = new List<QuoteItemForm>();
            foreach (DataGridViewRow row in lines.Rows)
            {
                if (row.IsNewRow) continue;
                var description = Convert.ToString(row.Cells["Description"].Value)?.Trim() ?? "";
                var unit = Convert.ToString(row.Cells["Unit"].Value)?.Trim() ?? "";
                if (!int.TryParse(Convert.ToString(row.Cells["Quantity"].Value), out var quantity) || quantity < 1
                    || !decimal.TryParse(Convert.ToString(row.Cells["UnitPrice"].Value), out var unitPrice) || unitPrice < 0 || description.Length == 0 || unit.Length == 0)
                { MessageBox.Show(this, "Kiểm tra hạng mục, đơn vị, số lượng và đơn giá."); return; }
                items.Add(new QuoteItemForm(description, unit, quantity, unitPrice, row.Cells["IncludedInPackage"].Value is true));
            }
            var form = new QuoteForm(items, deposit.Value, terms.Text.Trim(), expiry.Value);
            var errors = QuoteChecks.Validate(form);
            if (errors.Length > 0) { MessageBox.Show(this, errors); return; }
            Value = form;
            DialogResult = DialogResult.OK;
        };
        layout.Controls.Add(save); Controls.Add(layout);
    }
}

internal static class QuoteChecks
{
    // Kiểm tra sơ bộ phía máy trạm; quy tắc chính thức do API (QuoteCalculator) quyết định.
    public static string Validate(QuoteForm form)
    {
        if (form.Items.Count == 0) return "Cần ít nhất một hạng mục.";
        if (form.ValidUntil <= DateTime.Now) return "Hạn hiệu lực phải ở tương lai.";
        if (string.IsNullOrWhiteSpace(form.CancelPolicy)) return "Cần nhập điều kiện hủy.";
        return "";
    }
}

/// <summary>Hộp thoại nhập một lý do (hủy, từ chối, duyệt).</summary>
internal sealed class ReasonDialog : Form
{
    private readonly TextBox reason = new() { Multiline = true, Height = 90, Width = 400 };
    public string Reason => reason.Text.Trim();

    public ReasonDialog(string title, string prompt = "Lý do")
    {
        Text = title; Size = new Size(480, 300); StartPosition = FormStartPosition.CenterParent;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20) };
        layout.Controls.Add(Ui.Label(prompt)); layout.Controls.Add(reason);
        var ok = Ui.Button("Đồng ý", true);
        ok.Click += (_, _) => { if (Reason.Length == 0) { MessageBox.Show(this, "Cần nhập lý do."); return; } DialogResult = DialogResult.OK; };
        layout.Controls.Add(ok); Controls.Add(layout);
    }
}
