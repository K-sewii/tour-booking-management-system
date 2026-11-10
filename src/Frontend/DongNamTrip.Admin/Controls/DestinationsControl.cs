using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

/// <summary>SCR14 – Địa điểm và biểu mẫu thêm/sửa (UC14): thêm/sửa, công bố/ngừng công bố, xóa bản nháp hợp lệ.</summary>
public sealed class DestinationsControl : UserControl
{
    private static readonly (string Label, string? Code)[] StatusFilters =
    [
        ("Mọi trạng thái", null),
        ("Bản nháp", ContentStatus.Draft),
        ("Đã công bố", ContentStatus.Published),
        ("Ngừng công bố", ContentStatus.Inactive)
    ];

    private List<AdminDestination> items = [];
    private readonly DataGridView grid = Ui.Grid();
    private readonly TextBox search = new() { Width = 260, PlaceholderText = "Tìm tên, khu vực, địa chỉ…", Margin = new Padding(0, 10, 15, 5) };
    private readonly ComboBox statusFilter = new() { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 10, 15, 5) };
    private readonly Label count = Ui.Label("");

    public DestinationsControl(TripApiClient api, string token)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        statusFilter.Items.AddRange(StatusFilters.Select(x => (object)x.Label).ToArray());
        statusFilter.SelectedIndex = 0;
        var add = Ui.Button("+ Thêm địa điểm", true);
        var edit = Ui.Button("Sửa đã chọn");
        var publish = Ui.Button("Công bố");
        var unpublish = Ui.Button("Ngừng công bố");
        var delete = Ui.Button("Xóa bản nháp");
        var refresh = Ui.Button("Tải lại");
        toolbar.Controls.AddRange([search, statusFilter, add, edit, publish, unpublish, delete, refresh]);
        layout.Controls.Add(toolbar);
        layout.Controls.Add(grid);
        layout.Controls.Add(count);
        Controls.Add(layout);

        async Task Reload()
        {
            if (IsDisposed) return;
            var selected = Kit.SelectedId(grid);
            items = await api.AdminDestinationsAsync(token);
            if (IsDisposed) return;
            Filter();
            Kit.SelectRow(grid, selected);
        }

        void Filter()
        {
            var q = search.Text.Trim();
            var code = StatusFilters[Math.Max(0, statusFilter.SelectedIndex)].Code;
            var rows = items.Where(x => (code is null || x.Status == code)
                && (x.Name + " " + x.Region + " " + x.Address).Contains(q, StringComparison.CurrentCultureIgnoreCase)).ToList();
            grid.DataSource = rows.Select(x => new
            {
                x.Id,
                Tên_địa_điểm = x.Name,
                Khu_vực = x.Region,
                Loại_hình = x.Category,
                Trạng_thái = Kit.Status(x.Status),
                Hoạt_động = x.ActivityCount,
                Thuyết_minh = x.NarrationCount,
                Đã_gắn_tour_lịch_trình = x.HasTransactions ? "Có" : "Không"
            }).ToList();
            count.Text = $"{rows.Count}/{items.Count} địa điểm · Chỉ địa điểm \"Đã công bố\" mới hiển thị cho khách. Địa điểm đã gắn tour/lịch trình không xóa được, chỉ ngừng công bố.";
            if (grid.Columns.Contains("Id")) grid.Columns["Id"]!.Width = 55;
        }

        AdminDestination? Current() => Kit.SelectedId(grid) is { } id ? items.FirstOrDefault(x => x.Id == id) : null;

        async Task OpenEditor(AdminDestination? item)
        {
            DestinationForm? draft = null;
            while (true)
            {
                using var dialog = new DestinationEditForm(item, draft);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                draft = dialog.Value;
                try
                {
                    var saved = await api.SaveAdminDestinationAsync(item?.Id, draft, token);
                    await Reload();
                    Kit.SelectRow(grid, saved.Id);
                    return;
                }
                catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
                {
                    Kit.Warn(this, Kit.Describe(error), "Chưa lưu được");
                    if (error is HttpRequestException { StatusCode: null } or TaskCanceledException) return;
                }
            }
        }

        async Task ChangeStatus(string status, string confirm)
        {
            if (Current() is not { } item)
            {
                MessageBox.Show(this, "Chọn một địa điểm trước.");
                return;
            }

            if (!Kit.Confirm(this, $"{confirm}\n\n{item.Name}")) return;
            await Ui.RunAsync(this, async () =>
            {
                await api.SetDestinationStatusAsync(item.Id, status, token);
                await Reload();
            });
        }

        search.TextChanged += (_, _) => Filter();
        statusFilter.SelectedIndexChanged += (_, _) => Filter();
        refresh.Click += async (_, _) => await Ui.RunAsync(this, Reload);
        add.Click += async (_, _) => await SafeAsync(() => OpenEditor(null));
        edit.Click += async (_, _) =>
        {
            if (Current() is not { } item)
            {
                MessageBox.Show(this, "Chọn một địa điểm trước khi sửa.");
                return;
            }

            await SafeAsync(() => OpenEditor(item));
        };
        grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0 && Current() is { } item) await SafeAsync(() => OpenEditor(item));
        };
        publish.Click += async (_, _) => await ChangeStatus(ContentStatus.Published, "Công bố địa điểm để khách nhìn thấy?");
        unpublish.Click += async (_, _) => await ChangeStatus(ContentStatus.Inactive, "Ngừng công bố địa điểm? Dữ liệu và lịch sử vẫn được giữ lại.");
        delete.Click += async (_, _) =>
        {
            if (Current() is not { } item)
            {
                MessageBox.Show(this, "Chọn một bản nháp để xóa.");
                return;
            }

            if (!Kit.Confirm(this, $"Xóa bản nháp \"{item.Name}\"? Thao tác này không hoàn tác được.")) return;
            await Ui.RunAsync(this, async () =>
            {
                var result = await api.DeleteDestinationAsync(item.Id, token);
                if (!result.Deleted) Kit.Warn(this, result.Message, "Không xóa được");
                await Reload();
            });
        };
        Load += async (_, _) => await Ui.RunAsync(this, Reload);
    }

    private async Task SafeAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { Kit.Warn(this, Kit.Describe(error)); }
    }
}
