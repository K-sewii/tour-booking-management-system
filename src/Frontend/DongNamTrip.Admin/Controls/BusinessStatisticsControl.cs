using DongNamTrip.Client;
using DongNamTrip.Contracts;
using static DongNamTrip.Admin.Scr19Ui;

namespace DongNamTrip.Admin;

public sealed class BusinessStatisticsControl : UserControl
{
    private readonly TripApiClient api;
    private readonly string token;
    private readonly DateTimePicker from = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 130 };
    private readonly DateTimePicker to = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 130 };
    private readonly ComboBox orderStatus = new() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button apply = Ui.Button("Áp dụng", true);
    private readonly Label[] statKpis = [Ui.Label("—", 13, true), Ui.Label("—", 13, true), Ui.Label("—", 13, true), Ui.Label("—", 13, true)];
    private readonly DataGridView statistics = Ui.Grid();
    private readonly Label statisticsInfo = Ui.Label("", 9);
    private readonly Label requestInfo = Ui.Label("", 9);
    private readonly Label empty = new() { Dock = DockStyle.Fill, BackColor = Color.White, ForeColor = Ui.Muted, TextAlign = ContentAlignment.MiddleCenter, Visible = false };

    public BusinessStatisticsControl(TripApiClient api, string token)
    {
        this.api = api; this.token = token;
        var statsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(12), BackColor = Ui.Background };
        statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); statsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        statsLayout.Controls.Add(Ui.Label("Tổng quan kinh doanh", 17, true), 0, 0);
        statsLayout.Controls.Add(Bar(Ui.Label("Từ ngày"), from, Ui.Label("Đến ngày"), to, Ui.Label("Trạng thái"), orderStatus, apply), 0, 1);
        statsLayout.Controls.Add(Cards(statKpis, "Tổng số đơn đặt tour", "Giá trị đơn hiệu lực", "Tiền đã thu", "Tiền đã hoàn"), 0, 2);
        var tableContent = new Panel { Dock = DockStyle.Fill }; tableContent.Controls.Add(statistics); tableContent.Controls.Add(empty); empty.BringToFront();
        statsLayout.Controls.Add(tableContent, 0, 3); statsLayout.Controls.Add(Bar(statisticsInfo, requestInfo), 0, 4);
        Controls.Add(statsLayout);
        StyleGrid(statistics);
        var today = DateTime.UtcNow.AddHours(7).Date; from.Value = new DateTime(today.Year, today.Month, 1); to.Value = from.Value.AddMonths(1).AddDays(-1);
        orderStatus.Items.Add("Mọi trạng thái"); orderStatus.SelectedIndex = 0;
        apply.Click += async (_, _) => await LoadStatistics();
        Load += async (_, _) => await LoadStatistics();
    }
    private async Task LoadStatistics()
    {
        foreach (var value in statKpis) value.Text = "Đang tải…";
        requestInfo.Text = "";
        statistics.DataSource = null;
        if (from.Value.Date > to.Value.Date) { foreach (var value in statKpis) value.Text = "Chưa tải"; statisticsInfo.Text = ""; empty.Text = "Từ ngày phải không sau đến ngày."; empty.Visible = true; return; }
        apply.Enabled = false; statisticsInfo.Text = "Đang tải…"; empty.Text = "Đang tải thống kê…"; empty.Visible = true;
        try
        {
            var selected = orderStatus.SelectedIndex <= 0 ? "" : OrderStatus.All[orderStatus.SelectedIndex - 1];
            var result = await api.StatsAsync(from.Value.Date, to.Value.Date, selected, token);
            if (IsDisposed) return;
            orderStatus.Items.Clear(); orderStatus.Items.Add("Mọi trạng thái"); orderStatus.Items.AddRange(OrderStatus.All.Select(OrderStatus.Label).Cast<object>().ToArray());
            orderStatus.SelectedIndex = selected.Length == 0 ? 0 : Math.Max(0, OrderStatus.All.ToList().IndexOf(selected) + 1);
            string Money(decimal? amount) => amount?.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) ?? "Chưa xác định";
            statKpis[0].Text = result.Orders.Sum(x => (long)x.Count).ToString("N0"); statKpis[1].Text = Money(result.OrderValue) + " đ"; statKpis[2].Text = Money(result.Received) + " đ"; statKpis[3].Text = Money(result.Refunded) + " đ";
            statistics.DataSource = result.Orders.Select(x => new { Trạng_thái = OrderStatus.Label(x.Status), Số_đơn = x.Count, Giá_trị = x.OrderValue }).ToList();
            foreach (DataGridViewColumn column in statistics.Columns) column.HeaderText = column.HeaderText.Replace("_", " ");
            if (statistics.Columns["Giá_trị"] is { } moneyColumn) { moneyColumn.HeaderText = "Giá trị đơn hiệu lực"; moneyColumn.DefaultCellStyle.Format = "N0"; }
            statisticsInfo.Text = "Đơn theo ngày đặt; thu/hoàn theo ngày giao dịch, không lọc theo trạng thái đơn. Múi giờ Việt Nam (UTC+7).";
            empty.Text = "◯\r\nKhông có đơn phù hợp\r\nHãy thử khoảng thời gian hoặc trạng thái khác."; empty.Visible = result.Orders.Count == 0;
            requestInfo.Text = result.Requests.Count == 0 ? "Không có yêu cầu tour riêng" :
                "Yêu cầu tour riêng: " + string.Join(" · ", result.Requests.Select(x => $"{QuoteRequestStatus.Label(x.Status)}: {x.Count:N0}"));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { if (!Kit.HandleExpiredSession(this, e) && !IsDisposed) { foreach (var value in statKpis) value.Text = "Lỗi tải"; statisticsInfo.Text = ""; empty.Text = Kit.Describe(e) + " Hãy chọn Áp dụng để thử lại."; empty.Visible = true; } }
        finally { if (!IsDisposed) apply.Enabled = true; }
    }

}
