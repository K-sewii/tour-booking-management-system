using System.Globalization;
using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

/// <summary>SCR17: đơn đặt tour (Admin, Operator) và tài chính (Admin, Finance): xác nhận, điều chỉnh, duyệt hủy, hoàn thành, ghi thu hoàn.</summary>
public sealed class BookingsControl : UserControl
{
    private readonly DataGridView grid = Ui.Grid();
    private readonly Label status = Ui.Label("", 10);
    private readonly ComboBox statusFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly TextBox search = new() { Width = 200, PlaceholderText = "Mã đơn hoặc liên hệ" };
    private readonly CheckBox overdue = new() { Text = "Quá ngày đi, chưa hoàn thành", AutoSize = true, Margin = new Padding(10, 12, 10, 0) };
    private List<AdminBooking> orders = [];

    /// <param name="handleOrders">Hiện các nút xác nhận, điều chỉnh, duyệt hủy, hoàn thành (Admin, Operator).</param>
    /// <param name="handleMoney">Hiện nút ghi thu, hoàn (Admin, Finance).</param>
    public BookingsControl(TripApiClient api, string token, bool handleOrders, bool handleMoney)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 105));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var refresh = Ui.Button("Tải lại");
        var detail = Ui.Button("Chi tiết");
        statusFilter.Items.Add("(Tất cả)");
        statusFilter.Items.AddRange(OrderStatus.All.Select(OrderStatus.Label).Cast<object>().ToArray());
        statusFilter.SelectedIndex = Math.Max(0, OrderStatus.All.ToList().IndexOf(OrderStatus.Pending) + 1); // mặc định đơn Chờ xử lý
        toolbar.Controls.AddRange([statusFilter, search, overdue, refresh, detail]);
        var confirm = Ui.Button("Xác nhận đơn", true);
        var adjust = Ui.Button("Điều chỉnh");
        var approve = Ui.Button("Duyệt hủy");
        var reject = Ui.Button("Từ chối hủy");
        var complete = Ui.Button("Hoàn thành đơn");
        var receipt = Ui.Button("Ghi nhận thu", true);
        var refund = Ui.Button("Ghi nhận hoàn");
        if (handleOrders) toolbar.Controls.AddRange([confirm, adjust, approve, reject, complete]);
        if (handleMoney) toolbar.Controls.AddRange([receipt, refund]);
        layout.Controls.Add(toolbar, 0, 0); layout.Controls.Add(grid, 0, 1); layout.Controls.Add(status, 0, 2);
        Controls.Add(layout);

        async Task Reload()
        {
            var filter = new BookingFilter(statusFilter.SelectedIndex <= 0 ? null : OrderStatus.All[statusFilter.SelectedIndex - 1], null, null,
                string.IsNullOrWhiteSpace(search.Text) ? null : search.Text.Trim(), overdue.Checked ? true : null, 1, 50);
            var page = await api.AdminBookingsAsync(filter, token);
            orders = page.Items.ToList();
            if (IsDisposed) return;
            grid.DataSource = orders.Select(x => new { Mã = x.Id, Khách = x.Customer, Nguồn = BookingSources.Label(x.Source), Tour = x.Title, Ngày_đặt = x.PlacedAt,
                Số_khách = x.Guests, Giá_chốt = x.AgreedPrice, Đã_thu = x.Paid, Trạng_thái = OrderStatus.Label(x.Status) }).ToList();
            grid.Columns["Giá_chốt"]!.DefaultCellStyle.Format = "N0"; grid.Columns["Đã_thu"]!.DefaultCellStyle.Format = "N0";
            for (var i = 0; i < orders.Count && i < grid.Rows.Count; i++)
            {
                // Còn dưới 1 giờ giữ chỗ: tô hồng; đơn đã hủy hoặc hết hạn: chữ xám.
                if (orders[i].Status == OrderStatus.Pending && orders[i].HoldUntil is { } hold && hold - DateTime.Now < TimeSpan.FromHours(1)) grid.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                else if (orders[i].Status is OrderStatus.Cancelled or OrderStatus.Expired) grid.Rows[i].DefaultCellStyle.ForeColor = Color.Gray;
            }
            status.Text = $"{page.Total} đơn (hiện {orders.Count}). Đã thu = tổng thu; hoàn tiền hiển thị riêng ở chi tiết đơn.";
        }
        AdminBooking? Current() => grid.CurrentRow is null ? null : orders.FirstOrDefault(x => x.Id == Convert.ToInt32(grid.CurrentRow.Cells["Mã"].Value));
        Task Act(Func<AdminBooking, Task> action) => Current() is { } order ? Ui.RunAsync(this, async () => { await action(order); await Reload(); }) : Task.CompletedTask;

        refresh.Click += async (_, _) => await Ui.RunAsync(this, Reload);
        statusFilter.SelectedIndexChanged += async (_, _) => await Ui.RunAsync(this, Reload);
        overdue.CheckedChanged += async (_, _) => await Ui.RunAsync(this, Reload);
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) await Ui.RunAsync(this, Reload); };
        detail.Click += async (_, _) =>
        {
            if (Current() is not { } order) return;
            await Ui.RunAsync(this, async () =>
            {
                var d = await api.AdminBookingAsync(order.Id, token);
                var money = string.Join(Environment.NewLine, d.Detail.Transactions.Select(x => $"{MoneyKinds.Label(x.Kind)} {x.Amount:N0} · {x.At:dd/MM/yyyy HH:mm} · {x.Reference}"));
                var missing = d.MissingForConfirm.Count == 0 ? "" : "\n\nChưa đủ để xác nhận:\n" + string.Join(Environment.NewLine, d.MissingForConfirm);
                MessageBox.Show(this, $"Đơn #{order.Id} · {OrderStatus.Label(d.Booking.Status)}\n{d.Booking.Title} · {d.Booking.Guests} khách\nKhách: {d.Booking.Customer} · Liên hệ: {d.Detail.Contact}\nNgày đi: {d.Detail.Booking.TripDate:dd/MM/yyyy}\n" +
                    $"Giá chốt {d.Detail.Booking.AgreedPrice:N0} · Khoản cần thu {d.Detail.Booking.RequiredDeposit:N0}\nĐã thu {d.Detail.Paid:N0} · Đã hoàn {d.Detail.Refunded:N0} · Còn phải thu {d.Detail.Outstanding:N0} · Có thể hoàn {d.Detail.Refundable:N0}\n" +
                    $"Điều kiện hủy: {d.Detail.CancelPolicy}\nLý do hủy: {d.Detail.CancelReason}\n\nGiao dịch:\n{money}{missing}", "Chi tiết đơn");
            });
        };
        confirm.Click += async (_, _) => await Act(order => api.ConfirmBookingAsync(order.Id, token));
        complete.Click += async (_, _) => await Act(order => api.CompleteBookingAsync(order.Id, token));
        adjust.Click += async (_, _) => await Act(async order =>
        {
            using var dialog = new FieldsDialog($"Điều chỉnh đơn #{order.Id}",
            [
                new("Nội dung điều chỉnh", "", true), new("Lý do", "", true), new("Ý kiến đồng ý của khách", "", true), new("Giá chốt mới (VNĐ, để trống nếu không đổi)", "")
            ], v => v[0].Length == 0 || v[1].Length == 0 || v[2].Length == 0 ? "Cần nội dung, lý do và ý kiến đồng ý của khách."
                : v[3].Length > 0 && (!decimal.TryParse(v[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0) ? "Giá mới không hợp lệ." : null);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            var v = dialog.Values;
            await api.AdjustBookingAsync(order.Id, new AdjustForm(v[0], v[1], v[2], v[3].Length == 0 ? null : decimal.Parse(v[3], CultureInfo.InvariantCulture)), token);
        });
        approve.Click += async (_, _) => await Act(async order =>
        {
            using var dialog = new ReasonDialog($"Duyệt hủy đơn #{order.Id}", "Lý do duyệt");
            if (dialog.ShowDialog(this) == DialogResult.OK) await api.DecideCancelAsync(order.Id, new CancelDecision(true, dialog.Reason), token);
        });
        reject.Click += async (_, _) => await Act(async order =>
        {
            using var dialog = new ReasonDialog($"Từ chối hủy đơn #{order.Id}", "Lý do từ chối");
            if (dialog.ShowDialog(this) == DialogResult.OK) await api.DecideCancelAsync(order.Id, new CancelDecision(false, dialog.Reason), token);
        });

        async Task Money(AdminBooking order, string kind)
        {
            using var dialog = new FieldsDialog($"{MoneyKinds.Label(kind)} · đơn #{order.Id}",
            [
                new("Số tiền (VNĐ)", ""), new("Mã tham chiếu (duy nhất)", ""), new("Thời điểm (dd/MM/yyyy HH:mm)", DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
            ], v => !decimal.TryParse(v[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0 ? "Số tiền phải lớn hơn 0."
                : v[1].Length == 0 ? "Cần mã tham chiếu."
                : !DateTime.TryParseExact(v[2], "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ? "Thời điểm không đúng định dạng." : null);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            var v = dialog.Values;
            await api.RecordMoneyAsync(order.Id, new MoneyForm(kind, decimal.Parse(v[0], CultureInfo.InvariantCulture), v[1], DateTime.ParseExact(v[2], "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)), token);
        }
        receipt.Click += async (_, _) => await Act(order => Money(order, MoneyKinds.Receipt));
        refund.Click += async (_, _) => await Act(order => Money(order, MoneyKinds.Refund));
        Load += async (_, _) => await Ui.RunAsync(this, Reload);
    }
}
