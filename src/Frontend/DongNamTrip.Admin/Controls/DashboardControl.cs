using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

/// <summary>SCR13: tổng quan. Admin xem thêm thống kê vận hành và nhật ký thao tác (UC24).</summary>
public sealed class DashboardControl : UserControl
{
    public DashboardControl(TripApiClient api, string userName, string token, bool isAdmin)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(Ui.Label($"Chào {userName}, dữ liệu dưới đây được đọc từ API.", 12));
        var metrics = new FlowLayoutPanel { Dock = DockStyle.Fill };
        layout.Controls.Add(metrics);
        layout.Controls.Add(Ui.Label(isAdmin ? "Điểm đến, thống kê và nhật ký" : "Điểm đến trong danh mục", 15, true));
        var grid = Ui.Grid();
        if (isAdmin)
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var places = new TabPage("Điểm đến");
            places.Controls.Add(grid);
            tabs.TabPages.Add(places);
            tabs.TabPages.Add(StatsPage(api, token));
            tabs.TabPages.Add(AuditPage(api, token));
            layout.Controls.Add(tabs);
        }
        else layout.Controls.Add(grid);
        Controls.Add(layout);
        Load += async (_, _) => await Ui.RunAsync(this, async () =>
        {
            var destinations = await api.DestinationsAsync();
            var tours = await api.ToursAsync();
            if (IsDisposed)
                return;

            foreach (var text in new[]
            {
                $"{destinations.Count}  ĐỊA ĐIỂM",
                $"{tours.Count}  TOUR MẪU",
                "5  KHU VỰC DU LỊCH"
            })
            {
                var card = new Panel
                {
                    Width = 235,
                    Height = 80,
                    BackColor = Color.White,
                    Padding = new Padding(18),
                    Margin = new Padding(0, 0, 18, 0)
                };
                card.Controls.Add(Ui.Label(text, 13, true));
                metrics.Controls.Add(card);
            }

            grid.DataSource = destinations.Select(destination => new
            {
                Địa_điểm = destination.Name,
                Khu_vực = destination.Region,
                Loại_hình = destination.Category,
                Tham_quan_phút = destination.VisitMinutes
            }).ToList();
        });
    }

    private TabPage StatsPage(TripApiClient api, string token)
    {
        var page = new TabPage("Thống kê vận hành");
        var from = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Width = 120 };
        var to = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Width = 120 };
        var load = Ui.Button("Xem thống kê", true);
        var summary = Ui.Label("", 11);
        var orders = Ui.Grid();
        var requests = Ui.Grid();
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 56 };
        top.Controls.AddRange([Ui.Label("Từ ngày"), from, Ui.Label("đến"), to, load]);
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(summary, 0, 0); body.SetColumnSpan(summary, 2);
        body.Controls.Add(orders, 0, 1); body.Controls.Add(requests, 1, 1);
        page.Controls.Add(body); page.Controls.Add(top);
        load.Click += async (_, _) => await Ui.RunAsync(this, async () =>
        {
            var stats = await api.StatsAsync(from.Value.Date, to.Value.Date, null, token);
            summary.Text = $"Tổng giá trị đơn {stats.OrderValue:N0} ₫ · Đã thu {stats.Received:N0} ₫ · Đã hoàn {stats.Refunded:N0} ₫ (ba chỉ tiêu tách riêng)";
            orders.DataSource = stats.Orders.Select(x => new { Đơn_theo_trạng_thái = OrderStatus.Label(x.Status), Số_lượng = x.Count }).ToList();
            requests.DataSource = stats.Requests.Select(x => new { Yêu_cầu_tour_riêng = QuoteRequestStatus.Label(x.Status), Số_lượng = x.Count }).ToList();
            if (stats.Orders.Count == 0 && stats.Requests.Count == 0) summary.Text += " · Không có dữ liệu trong khoảng này.";
        });
        return page;
    }

    private TabPage AuditPage(TripApiClient api, string token)
    {
        var page = new TabPage("Nhật ký thao tác");
        var grid = Ui.Grid();
        var objectType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        objectType.Items.Add("(Mọi đối tượng)");
        objectType.Items.AddRange(["TOUR", "LICHKHOIHANH", "DONDAT", "GIAODICHTIEN", "YEUCAUTOURRIENG", "XACMINHDICHVU", "BAOGIA", "TAIKHOAN", "DIADIEM", "HOATDONG", "SUAT"]);
        objectType.SelectedIndex = 0;
        var load = Ui.Button("Tải nhật ký", true);
        var older = Ui.Button("Trang sau");
        var newer = Ui.Button("Trang trước");
        var info = Ui.Label("");
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 56 };
        top.Controls.AddRange([objectType, load, newer, older, info]);
        page.Controls.Add(grid); page.Controls.Add(top);
        var number = 1;
        async Task Reload()
        {
            var result = await api.AuditAsync(new AuditFilter(null, null, objectType.SelectedIndex <= 0 ? null : objectType.Text, null, number, 25), token);
            grid.DataSource = result.Items.Select(x => new { Thời_điểm = x.At, Người_thực_hiện = x.ActorName, Thao_tác = x.Action, Đối_tượng = x.ObjectType, Mã = x.ObjectId, Lý_do = x.Reason }).ToList();
            info.Text = $"Trang {result.Page} · {result.Total} dòng (chỉ đọc)";
        }
        load.Click += async (_, _) => { number = 1; await Ui.RunAsync(this, Reload); };
        older.Click += async (_, _) => { number++; await Ui.RunAsync(this, Reload); };
        newer.Click += async (_, _) => { number = Math.Max(1, number - 1); await Ui.RunAsync(this, Reload); };
        return page;
    }
}
