using DongNamTrip.Client;

namespace DongNamTrip.Admin;
public sealed class DashboardControl : UserControl
{
    public DashboardControl(TripApiClient api)
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
        layout.Controls.Add(Ui.Label("Chào Đức, cùng chuẩn bị những hành trình mới.\nDữ liệu bên dưới được đọc trực tiếp từ API demo.", 12));
        var metrics = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill
        };
        layout.Controls.Add(metrics);
        layout.Controls.Add(Ui.Label("Điểm đến trong danh mục", 15, true));
        var grid = Ui.Grid();
        layout.Controls.Add(grid);
        Controls.Add(layout);
        Load += async (_, _) => await Ui.RunAsync(this, async () =>
        {
            var destinations = await api.DestinationsAsync();
            var tours = await api.ToursAsync();
            if (IsDisposed) return;
            foreach (var text in new[]
            {
                $"{destinations.Count}  ĐỊA ĐIỂM",
                $"{tours.Count}  TOUR MẪU",
                "5  KHU VỰC DU LỊCH"
            }

            )
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

            grid.DataSource = destinations.Select(x => new { Địa_điểm = x.Name, Khu_vực = x.Region, Loại_hình = x.Category, Phút_tham_quan = x.VisitMinutes }).ToList();
        });
    }
}
