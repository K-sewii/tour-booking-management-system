using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

public sealed class PrivateToursControl : UserControl
{
    private readonly DataGridView grid = Ui.Grid();
    private readonly Label status = Ui.Label("");
    private List<PrivateTourRequest> requests = [];

    public PrivateToursControl(TripApiClient api, string token)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var refresh = Ui.Button("Tải lại");
        var detail = Ui.Button("Xem yêu cầu");
        var verify = Ui.Button("Ghi xác minh");
        var quote = Ui.Button("Phát hành báo giá", true);
        actions.Controls.AddRange([refresh, detail, verify, quote]);
        layout.Controls.Add(actions, 0, 0); layout.Controls.Add(grid, 0, 1); layout.Controls.Add(status, 0, 2); Controls.Add(layout);

        async Task Reload()
        {
            requests = await api.StaffPrivateTourRequestsAsync(token);
            if (IsDisposed) return;
            grid.DataSource = requests.Select(x => new { x.Id, Khách_hàng = x.CustomerName, Lịch_trình = x.Snapshot.Name,
                Ngày_đi = x.Snapshot.StartDate, Số_khách = x.Snapshot.Guests, Trạng_thái = x.Status, Phiên_bản = x.Version }).ToList();
            status.Text = $"{requests.Count} yêu cầu · Suất khách chọn chỉ là mong muốn, cần xác minh với nhà cung cấp.";
        }
        PrivateTourRequest? Current() => grid.CurrentRow is null ? null : requests.FirstOrDefault(x => x.Id == Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value));

        refresh.Click += async (_, _) => await Ui.RunAsync(this, Reload);
        detail.Click += (_, _) =>
        {
            var request = Current(); if (request is null) return;
            var stops = string.Join(Environment.NewLine, request.Snapshot.Stops.Select(x => $"Ngày {x.Day}, thứ tự {x.Order}: điểm #{x.DestinationId}; suất {string.Join(", ", x.SlotIds)}; {x.Note}"));
            var checks = string.Join(Environment.NewLine, request.Verifications.Select(x => $"{x.Item}: {x.Result} · {x.Supplier} · {x.Note}"));
            MessageBox.Show(this, $"Yêu cầu #{request.Id} · Phiên bản {request.Version}\nKhách: {request.CustomerName}\nLiên hệ: {request.Contact}\nNhu cầu: {request.ServiceNeeds}\nNgày đi: {request.Snapshot.StartDate:dd/MM/yyyy} · {request.Snapshot.Guests} khách\nXuất phát: {request.Snapshot.DeparturePoint} · {request.Snapshot.Transport}\n\n{stops}\n\nCảnh báo:\n{string.Join(Environment.NewLine, request.Snapshot.Warnings)}\n\nXác minh:\n{checks}", "Chi tiết tour riêng");
        };
        verify.Click += async (_, _) =>
        {
            var request = Current(); if (request is null) return;
            using var dialog = new VerificationForm(); if (dialog.ShowDialog(this) != DialogResult.OK) return;
            await Ui.RunAsync(this, async () => { await api.VerifyServiceAsync(request.Id, dialog.Value, token); await Reload(); });
        };
        quote.Click += async (_, _) =>
        {
            var request = Current(); if (request is null) return;
            using var dialog = new QuoteForm(); if (dialog.ShowDialog(this) != DialogResult.OK) return;
            await Ui.RunAsync(this, async () => { await api.IssueQuoteAsync(request.Id, dialog.Value, token); await Reload(); });
        };
        Load += async (_, _) => await Ui.RunAsync(this, Reload);
    }
}

internal sealed class VerificationForm : Form
{
    private readonly TextBox item = new(), supplier = new(), note = new() { Multiline = true, Height = 65 };
    private readonly ComboBox result = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    public ServiceVerificationDraft Value => new() { Item = item.Text.Trim(), Supplier = supplier.Text.Trim(), Result = result.Text, Note = note.Text.Trim() };

    public VerificationForm()
    {
        Text = "Xác minh dịch vụ"; Size = new Size(480, 400); StartPosition = FormStartPosition.CenterParent;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(20) };
        void Add(string label, Control control) { control.Width = 400; layout.Controls.Add(Ui.Label(label)); layout.Controls.Add(control); }
        Add("Hạng mục cần xác minh", item); Add("Đơn vị cung cấp", supplier);
        result.Items.AddRange(["Available", "Unavailable", "NeedsInfo"]); result.SelectedIndex = 0; Add("Kết quả", result); Add("Nguồn liên hệ / phương án thay thế / ghi chú", note);
        var save = Ui.Button("Lưu xác minh", true); save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(item.Text) || string.IsNullOrWhiteSpace(supplier.Text)) { MessageBox.Show(this, "Nhập hạng mục và đơn vị cung cấp."); return; }
            DialogResult = DialogResult.OK;
        };
        layout.Controls.Add(save); Controls.Add(layout);
    }
}

internal sealed class QuoteForm : Form
{
    private readonly DateTimePicker expiry = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Value = DateTime.Now.AddDays(7) };
    private readonly NumericUpDown amountDue = new() { Maximum = 999999999999999999, ThousandsSeparator = true };
    private readonly TextBox terms = new() { Multiline = true, Height = 55 };
    private readonly DataGridView lines = new() { AllowUserToAddRows = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Height = 250 };
    public QuoteDraft Value { get; private set; } = new();

    public QuoteForm()
    {
        Text = "Phát hành báo giá"; Size = new Size(760, 620); StartPosition = FormStartPosition.CenterParent;
        lines.Columns.Add("Description", "Hạng mục"); lines.Columns.Add("Unit", "Đơn vị"); lines.Columns.Add("Quantity", "Số lượng"); lines.Columns.Add("UnitPrice", "Đơn giá (VNĐ)");
        lines.Rows.Add("Dịch vụ tour riêng", "gói", "1", "0");
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(20) };
        layout.Controls.Add(Ui.Label("Hạng mục, số lượng và đơn giá (không nhập lại mục đã nằm trong giá trọn gói)")); lines.Width = 680; layout.Controls.Add(lines);
        void Add(string label, Control control) { control.Width = 680; layout.Controls.Add(Ui.Label(label)); layout.Controls.Add(control); }
        Add("Hạn hiệu lực", expiry); Add("Khoản cần thu (VNĐ)", amountDue); Add("Điều kiện hủy", terms);
        var save = Ui.Button("Phát hành", true); save.Click += (_, _) =>
        {
            lines.EndEdit();
            var items = new List<QuoteLineDraft>();
            foreach (DataGridViewRow row in lines.Rows)
            {
                if (row.IsNewRow) continue;
                var description = Convert.ToString(row.Cells["Description"].Value)?.Trim() ?? "";
                var unit = Convert.ToString(row.Cells["Unit"].Value)?.Trim() ?? "";
                if (!int.TryParse(Convert.ToString(row.Cells["Quantity"].Value), out var quantity) || quantity < 1
                    || !decimal.TryParse(Convert.ToString(row.Cells["UnitPrice"].Value), out var unitPrice) || unitPrice < 0
                    || description.Length == 0 || unit.Length == 0)
                { MessageBox.Show(this, "Kiểm tra hạng mục, đơn vị, số lượng và đơn giá."); return; }
                items.Add(new QuoteLineDraft { Description = description, Unit = unit, Quantity = quantity, UnitPrice = unitPrice });
            }
            if (items.Count == 0 || expiry.Value <= DateTime.Now || string.IsNullOrWhiteSpace(terms.Text)
                || amountDue.Value > items.Sum(x => x.Quantity * x.UnitPrice))
            { MessageBox.Show(this, "Cần hạng mục, hạn hiệu lực tương lai, điều kiện hủy và khoản cần thu hợp lệ."); return; }
            Value = new QuoteDraft { Lines = [..items], ExpiresAt = expiry.Value.ToUniversalTime(), AmountDue = amountDue.Value, CancellationTerms = terms.Text.Trim() };
            DialogResult = DialogResult.OK;
        };
        layout.Controls.Add(save); Controls.Add(layout);
    }
}
