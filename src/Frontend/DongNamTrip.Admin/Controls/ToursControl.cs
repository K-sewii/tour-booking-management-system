using System.Globalization;
using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

/// <summary>SCR16: quản trị tour, điểm dừng và lịch khởi hành (UC16).</summary>
public sealed class ToursControl : UserControl
{
    private readonly DataGridView grid = Ui.Grid();
    private readonly Label status = Ui.Label("", 10);
    private List<AdminTour> tours = [];

    public ToursControl(TripApiClient api, string token)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var refresh = Ui.Button("Tải lại");
        var add = Ui.Button("Thêm tour", true);
        var edit = Ui.Button("Sửa tour");
        var stops = Ui.Button("Điểm dừng");
        var publish = Ui.Button("Công bố");
        var deactivate = Ui.Button("Ngừng tour");
        var departures = Ui.Button("Lịch khởi hành");
        toolbar.Controls.AddRange([refresh, add, edit, stops, publish, deactivate, departures]);
        layout.Controls.Add(toolbar, 0, 0); layout.Controls.Add(grid, 0, 1); layout.Controls.Add(status, 0, 2);
        Controls.Add(layout);

        async Task Reload()
        {
            tours = await api.AdminToursAsync(null, token);
            if (IsDisposed) return;
            grid.DataSource = tours.Select(x => new { Mã = x.Id, Tên_tour = x.Name, Khu_vực = x.Region, Loại_hình = x.Category, Thời_lượng = x.Duration,
                Giá_tham_khảo = x.ReferencePrice, Trạng_thái = ContentStatus.Label(x.Status), Điểm_dừng = x.Stops, Lịch_mở = x.OpenDepartures }).ToList();
            grid.Columns["Giá_tham_khảo"]!.DefaultCellStyle.Format = "N0";
            status.Text = $"{tours.Count} tour. Tour chỉ hiển thị cho khách khi đã công bố; ngừng tour không ảnh hưởng đơn cũ.";
        }
        AdminTour? Current() => grid.CurrentRow is null ? null : tours.FirstOrDefault(x => x.Id == Convert.ToInt32(grid.CurrentRow.Cells["Mã"].Value));

        async Task Save(AdminTour? existing)
        {
            AdminTourDetail? detail = existing is null ? null : await api.AdminTourAsync(existing.Id, token);
            using var dialog = new FieldsDialog(existing is null ? "Thêm tour" : "Sửa tour",
            [
                new("Tên tour", existing?.Name ?? ""),
                new("Khu vực", existing?.Region ?? "", Options: CatalogOptions.Regions),
                new("Loại hình", existing?.Category ?? "", Options: CatalogOptions.Categories),
                new("Thời lượng (ví dụ 1 ngày)", existing?.Duration ?? ""),
                new("Giá tham khảo (VNĐ / khách)", existing is null ? "0" : existing.ReferencePrice.ToString("0", CultureInfo.InvariantCulture)),
                new("Tóm tắt", detail?.Summary ?? "", true),
                new("Điều kiện hủy", detail?.CancelPolicy ?? "", true)
            ], values => values[0].Length == 0 || values[3].Length == 0 ? "Nhập tên tour và thời lượng."
                : !decimal.TryParse(values[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0 ? "Giá tham khảo không hợp lệ." : null);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            var v = dialog.Values;
            await api.SaveAdminTourAsync(existing?.Id, new TourForm(v[0], v[1], v[2], v[3], decimal.Parse(v[4], CultureInfo.InvariantCulture), v[5], v[6].Length == 0 ? null : v[6]), token);
            await Reload();
        }

        refresh.Click += async (_, _) => await Ui.RunAsync(this, Reload);
        add.Click += async (_, _) => await Ui.RunAsync(this, () => Save(null));
        edit.Click += async (_, _) => { if (Current() is { } tour) await Ui.RunAsync(this, () => Save(tour)); };
        publish.Click += async (_, _) => { if (Current() is { } tour) await Ui.RunAsync(this, async () => { await api.SetTourStatusAsync(tour.Id, TourStatus.Published, null, token); await Reload(); }); };
        deactivate.Click += async (_, _) =>
        {
            if (Current() is not { } tour || !Kit.Confirm(this, $"Ngừng tour \"{tour.Name}\"? Tour sẽ không còn nhận giữ chỗ mới; đơn cũ không bị ảnh hưởng.")) return;
            await Ui.RunAsync(this, async () => { await api.SetTourStatusAsync(tour.Id, TourStatus.Inactive, null, token); await Reload(); });
        };
        stops.Click += async (_, _) =>
        {
            if (Current() is not { } tour) return;
            await Ui.RunAsync(this, async () =>
            {
                var detail = await api.AdminTourAsync(tour.Id, token);
                var places = (await api.AdminDestinationsAsync(token)).Where(x => x.Status == ContentStatus.Published).ToList();
                using var dialog = new TourStopsDialog(tour.Name, places, detail.Stops);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                await api.SetTourPlacesAsync(tour.Id, dialog.Value, token);
                await Reload();
            });
        };
        departures.Click += async (_, _) =>
        {
            if (Current() is not { } tour) return;
            using var dialog = new DeparturesDialog(api, token, tour);
            dialog.ShowDialog(this);
            await Ui.RunAsync(this, Reload);
        };
        Load += async (_, _) => await Ui.RunAsync(this, Reload);
    }
}

internal sealed class TourStopsDialog : Form
{
    private readonly DataGridView grid = new() { AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Dock = DockStyle.Fill };

    public TourStopsDialog(string title, IReadOnlyList<AdminDestination> places, IReadOnlyList<TourStop> current)
    {
        Text = "Điểm dừng · " + title; Size = new Size(720, 560); StartPosition = FormStartPosition.CenterParent;
        grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Chosen", HeaderText = "Chọn", FillWeight = 12 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Mã", ReadOnly = true, FillWeight = 12 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Địa điểm (đã công bố)", ReadOnly = true, FillWeight = 56 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Day", HeaderText = "Ngày thứ", FillWeight = 10 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Order", HeaderText = "Thứ tự", FillWeight = 10 });
        foreach (var place in places)
        {
            var stop = current.FirstOrDefault(x => x.DestinationId == place.Id);
            grid.Rows.Add(stop is not null, place.Id, place.Name, stop?.Day ?? 1, stop?.Order ?? 1);
        }
        var save = Ui.Button("Lưu điểm dừng", true);
        save.Click += (_, _) =>
        {
            grid.EndEdit();
            var result = new List<TourStopForm>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Cells["Chosen"].Value is not true) continue;
                if (!int.TryParse(Convert.ToString(row.Cells["Day"].Value), out var day) || !int.TryParse(Convert.ToString(row.Cells["Order"].Value), out var order) || day < 1 || order < 1)
                { MessageBox.Show(this, "Ngày thứ và thứ tự phải là số từ 1."); return; }
                result.Add(new TourStopForm(Convert.ToInt32(row.Cells["Id"].Value), day, order));
            }
            Value = result;
            DialogResult = DialogResult.OK;
        };
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56 };
        bottom.Controls.Add(save);
        Controls.Add(grid); Controls.Add(bottom);
    }

    public IReadOnlyList<TourStopForm> Value { get; private set; } = [];
}

internal sealed class DeparturesDialog : Form
{
    private readonly DataGridView grid = Ui.Grid();
    private List<AdminDeparture> items = [];

    public DeparturesDialog(TripApiClient api, string token, AdminTour tour)
    {
        Text = "Lịch khởi hành · " + tour.Name; Size = new Size(900, 560); StartPosition = FormStartPosition.CenterParent;
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 56 };
        var add = Ui.Button("Thêm lịch", true);
        var edit = Ui.Button("Sửa lịch");
        var stop = Ui.Button("Ngừng lịch");
        toolbar.Controls.AddRange([add, edit, stop]);
        Controls.Add(grid); Controls.Add(toolbar);

        async Task Reload()
        {
            items = (await api.AdminTourAsync(tour.Id, token)).Departures.ToList();
            grid.DataSource = items.Select(x => new { Mã = x.Id, Khởi_hành = x.StartsAt, Giá = x.Price, Sức_chứa = x.Capacity, Đã_giữ = x.Held, Trạng_thái = DepartureStatus.Label(x.Status) }).ToList();
            grid.Columns["Giá"]!.DefaultCellStyle.Format = "N0";
        }
        AdminDeparture? Current() => grid.CurrentRow is null ? null : items.FirstOrDefault(x => x.Id == Convert.ToInt32(grid.CurrentRow.Cells["Mã"].Value));

        async Task Save(AdminDeparture? existing)
        {
            using var dialog = new FieldsDialog(existing is null ? "Thêm lịch khởi hành" : "Sửa lịch khởi hành",
            [
                new("Ngày giờ khởi hành (dd/MM/yyyy HH:mm)", (existing?.StartsAt ?? DateTime.Now.AddDays(14)).ToString("dd/MM/yyyy HH:mm")),
                new("Giá / khách (VNĐ)", existing is null ? "" : existing.Price.ToString("0", CultureInfo.InvariantCulture)),
                new("Sức chứa", existing?.Capacity.ToString() ?? "")
            ], v => !DateTime.TryParseExact(v[0], "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ? "Ngày giờ không đúng định dạng dd/MM/yyyy HH:mm."
                : !decimal.TryParse(v[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0 ? "Giá phải lớn hơn 0."
                : !int.TryParse(v[2], out var capacity) || capacity <= 0 ? "Sức chứa phải lớn hơn 0." : null);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            var v = dialog.Values;
            var form = new DepartureForm(DateTime.ParseExact(v[0], "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture), decimal.Parse(v[1], CultureInfo.InvariantCulture), int.Parse(v[2]));
            if (existing is null) await api.CreateDepartureAsync(tour.Id, form, token); else await api.UpdateDepartureAsync(existing.Id, form, token);
            await Reload();
        }

        add.Click += async (_, _) => await Ui.RunAsync(this, () => Save(null));
        edit.Click += async (_, _) => { if (Current() is { } item) await Ui.RunAsync(this, () => Save(item)); };
        stop.Click += async (_, _) =>
        {
            if (Current() is not { } item || !Kit.Confirm(this, "Ngừng lịch khởi hành này? Thao tác một chiều; đơn đã đặt không bị ảnh hưởng.")) return;
            await Ui.RunAsync(this, async () => { await api.DeactivateDepartureAsync(item.Id, token); await Reload(); });
        };
        Load += async (_, _) => await Ui.RunAsync(this, Reload);
    }
}
