using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;
public sealed class DestinationsControl : UserControl
{
    private List<Destination> items = [];
    private readonly DataGridView grid = Ui.Grid();
    private readonly TextBox search = new()
    {
        Width = 300,
        PlaceholderText = "Tìm tên hoặc khu vực…",
        Margin = new Padding(0, 10, 15, 5)
    };
    private readonly Label count = Ui.Label("");
    public DestinationsControl(TripApiClient api, string token)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill
        };
        var add = Ui.Button("+ Thêm địa điểm", true);
        var edit = Ui.Button("Sửa đã chọn");
        var refresh = Ui.Button("Tải lại");
        toolbar.Controls.AddRange([search, add, edit, refresh]);
        layout.Controls.Add(toolbar);
        layout.Controls.Add(grid);
        layout.Controls.Add(count);
        Controls.Add(layout);
        async Task Reload()
        {
            if (IsDisposed) return;
            items = await api.DestinationsAsync();
            if (!IsDisposed) Filter();
        }

        void Filter()
        {
            var q = search.Text.Trim();
            grid.DataSource = items.Where(x => (x.Name + " " + x.Region).Contains(q, StringComparison.CurrentCultureIgnoreCase)).Select(x => new { x.Id, Tên_địa_điểm = x.Name, Khu_vực = x.Region, Loại_hình = x.Category, Tham_quan_phút = x.VisitMinutes }).ToList();
            count.Text = $"{grid.Rows.Count} địa điểm · Thêm/sửa qua API, Web tải lại sẽ thấy dữ liệu mới.";
            if (grid.Columns.Contains("Id"))
                grid.Columns["Id"]!.Width = 55;
        }

        async Task OpenEditor(Destination? item)
        {
            using var dialog = new DestinationEditForm(item);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;
            await Ui.RunAsync(this, async () =>
            {
                await api.SaveDestinationAsync(item?.Id, dialog.Value, token);
                await Reload();
            });
        }

        search.TextChanged += (_, _) => Filter();
        refresh.Click += async (_, _) => await Ui.RunAsync(this, Reload);
        add.Click += async (_, _) => await OpenEditor(null);
        edit.Click += async (_, _) =>
        {
            if (grid.CurrentRow is null)
            {
                MessageBox.Show(this, "Chọn một địa điểm trước khi sửa.");
                return;
            }

            var id = Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
            await OpenEditor(items.Single(x => x.Id == id));
        };
        Load += async (_, _) => await Ui.RunAsync(this, Reload);
    }
}
