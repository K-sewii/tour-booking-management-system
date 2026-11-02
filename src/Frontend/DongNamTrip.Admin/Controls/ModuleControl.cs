namespace DongNamTrip.Admin;
// Màn hình nền: tách thành UserControl riêng khi triển khai mô đun.
public sealed class ModuleControl : UserControl
{
    public ModuleControl(string title, string owner, string[] columns)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill
        };
        var search = new TextBox
        {
            Width = 280,
            PlaceholderText = "Tìm kiếm · Chưa triển khai",
            Enabled = false,
            Margin = new Padding(0, 12, 15, 0)
        };
        var add = Ui.Button("Thêm mới · Chưa triển khai");
        add.Enabled = false;
        toolbar.Controls.Add(search);
        toolbar.Controls.Add(add);
        layout.Controls.Add(toolbar);
        var grid = Ui.Grid();
        foreach (var column in columns)
            grid.Columns.Add(column, column);
        layout.Controls.Add(grid);
        layout.Controls.Add(Ui.Label($"{title} — chưa có dữ liệu.\nMàn hình nền đã sẵn sàng. {owner} tiếp tục bổ sung biểu mẫu, API và nghiệp vụ.", 11));
        Controls.Add(layout);
    }
}
