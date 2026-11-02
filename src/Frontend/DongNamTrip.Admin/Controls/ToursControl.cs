using DongNamTrip.Client;

namespace DongNamTrip.Admin;
public sealed class ToursControl : UserControl
{
    public ToursControl(TripApiClient api)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill
        };
        var refresh = Ui.Button("Tải lại");
        var add = Ui.Button("Thêm tour · Chưa triển khai");
        add.Enabled = false;
        toolbar.Controls.Add(refresh);
        toolbar.Controls.Add(add);
        layout.Controls.Add(toolbar);
        var grid = Ui.Grid();
        layout.Controls.Add(grid);
        layout.Controls.Add(Ui.Label("LỊCH KHỞI HÀNH\nChưa có lịch được công bố. Kỳ bổ sung màn hình thêm/sửa tour,\nlịch khởi hành và nghiệp vụ đặt tour tại mô đun này.", 11));
        Controls.Add(layout);
        async Task Reload()
        {
            var tours = await api.ToursAsync();
            if (IsDisposed) return;
            grid.DataSource = tours.Select(x => new { Mã = x.Id, Tên_tour = x.Name, Khu_vực = x.Region, Thời_lượng = x.Duration, Giá_tham_khảo = x.ReferencePrice }).ToList();
            grid.Columns["Giá_tham_khảo"]!.DefaultCellStyle.Format = "N0";
        }

        Load += async (_, _) => await Ui.RunAsync(this, Reload);
        refresh.Click += async (_, _) => await Ui.RunAsync(this, Reload);
    }
}
