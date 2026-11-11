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
    private readonly Label[] statKpis = [Ui.Label("—", 13, true), Ui.Label("—", 13, true), Ui.Label("—", 13, true), Ui.Label("—", 13, true), Ui.Label("—", 13, true)];
    private readonly DataGridView statistics = Ui.Grid();
    private readonly Label statisticsInfo = Ui.Label("", 9);
    private readonly Label empty = new() { Dock = DockStyle.Fill, BackColor = Color.White, ForeColor = Ui.Muted, TextAlign = ContentAlignment.MiddleCenter, Visible = false };

    public BusinessStatisticsControl(TripApiClient api, string token)
    {
        this.api = api; this.token = token;
        var statsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(12), BackColor = Ui.Background };
        statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); statsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        statsLayout.Controls.Add(Ui.Label("Tổng quan kinh doanh", 17, true), 0, 0);
        statsLayout.Controls.Add(Bar(Ui.Label("Từ ngày"), from, Ui.Label("Đến ngày"), to, Ui.Label("Trạng thái"), orderStatus, apply), 0, 1);
        statsLayout.Controls.Add(Cards(statKpis, "Tổng số đơn đặt tour", "Tổng giá trị đơn", "Tiền đã thu", "Tiền đã hoàn", "Lượt nghe audio"), 0, 2);
        var tableContent = new Panel { Dock = DockStyle.Fill }; tableContent.Controls.Add(statistics); tableContent.Controls.Add(empty); empty.BringToFront();
        statsLayout.Controls.Add(tableContent, 0, 3); statsLayout.Controls.Add(statisticsInfo, 0, 4);
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
        statistics.DataSource = null;
        if (from.Value.Date > to.Value.Date) { foreach (var value in statKpis) value.Text = "Chưa tải"; statisticsInfo.Text = ""; empty.Text = "Từ ngày phải không sau đến ngày."; empty.Visible = true; return; }
        apply.Enabled = false; statisticsInfo.Text = "Đang tải…"; empty.Text = "Đang tải thống kê…"; empty.Visible = true;
        try
        {
            var selected = orderStatus.SelectedIndex <= 0 ? "" : orderStatus.Text;
            var result = await api.AdminStatisticsAsync(DateOnly.FromDateTime(from.Value), DateOnly.FromDateTime(to.Value), selected, token);
            if (IsDisposed) return;
            orderStatus.Items.Clear(); orderStatus.Items.Add("Mọi trạng thái"); orderStatus.Items.AddRange(result.Statuses.Cast<object>().ToArray());
            orderStatus.SelectedIndex = selected.Length == 0 ? 0 : Math.Max(0, orderStatus.Items.IndexOf(selected));
            string Money(decimal? amount) => amount?.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) ?? "Chưa xác định";
            statKpis[0].Text = result.Rows.Sum(x => (long)x.Orders).ToString("N0"); statKpis[1].Text = Money(result.OrderValue) + " đ"; statKpis[2].Text = result.Collected is null ? "Chưa xác định" : Money(result.Collected) + " đ"; statKpis[3].Text = result.Refunded is null ? "Chưa xác định" : Money(result.Refunded) + " đ"; statKpis[4].Text = result.Listens?.ToString("N0") ?? "Chưa ghi nhận";
            statistics.DataSource = result.Rows.Select(x => new { Trạng_thái = x.Status, Số_đơn = x.Orders, Giá_trị = x.OrderValue }).ToList();
            foreach (DataGridViewColumn column in statistics.Columns) column.HeaderText = column.HeaderText.Replace("_", " ");
            if (statistics.Columns["Giá_trị"] is { } moneyColumn) moneyColumn.DefaultCellStyle.Format = "N0";
            statisticsInfo.Text = result.Rows.Count == 0 ? "" : "Theo ngày đặt; múi giờ Việt Nam (UTC+7).";
            empty.Text = "◯\r\nKhông có đơn phù hợp\r\nHãy thử khoảng thời gian hoặc trạng thái khác."; empty.Visible = result.Rows.Count == 0;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { if (!IsDisposed) { foreach (var value in statKpis) value.Text = "Lỗi tải"; statisticsInfo.Text = ""; empty.Text = Kit.Describe(e) + " Hãy chọn Áp dụng để thử lại."; empty.Visible = true; } }
        finally { if (!IsDisposed) apply.Enabled = true; }
    }

}
