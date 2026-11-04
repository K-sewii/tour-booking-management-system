using System.ComponentModel.DataAnnotations;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;
public sealed class DestinationEditForm : Form
{
    public DestinationForm Value { get; private set; } = new();

    /// <param name="initial">Dữ liệu đã nhập trước đó, dùng để giữ nguyên khi API từ chối và mở lại biểu mẫu.</param>
    public DestinationEditForm(AdminDestination? item, DestinationForm? initial = null)
    {
        Text = item is null ? "Thêm địa điểm" : "Sửa địa điểm";
        ClientSize = new Size(640, 760);
        MinimumSize = new Size(640, 760);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Ui.Background;
        var source = initial ?? (item is null ? new DestinationForm() : new DestinationForm
        {
            Name = item.Name,
            Region = item.Region,
            Category = item.Category,
            Address = item.Address,
            Description = item.Description,
            VisitMinutes = item.VisitMinutes,
            OpenTime = item.OpenTime,
            CloseTime = item.CloseTime,
            Latitude = item.Latitude,
            Longitude = item.Longitude
        });
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(25), ColumnCount = 2, RowCount = 12 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 9; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));

        var name = new TextBox { Text = source.Name, Dock = DockStyle.Fill, MaxLength = 120 };
        var region = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        region.Items.AddRange(CatalogOptions.Regions);
        region.SelectedItem = CatalogOptions.Regions.Contains(source.Region) ? source.Region : CatalogOptions.Regions[0];
        var category = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        category.Items.AddRange(CatalogOptions.Categories);
        category.SelectedItem = CatalogOptions.Categories.Contains(source.Category) ? source.Category : CatalogOptions.Categories[0];
        var address = new TextBox { Text = source.Address, Dock = DockStyle.Fill, MaxLength = 250 };
        var minutes = new NumericUpDown { Minimum = 15, Maximum = 1440, Increment = 15, Value = Math.Clamp(source.VisitMinutes, 15, 1440), Dock = DockStyle.Fill };
        var open = TimePicker(source.OpenTime, 7);
        var close = TimePicker(source.CloseTime, 17);
        var latitude = new TextBox { Text = source.Latitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "", Dock = DockStyle.Fill, PlaceholderText = "Ví dụ 10.779783" };
        var longitude = new TextBox { Text = source.Longitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "", Dock = DockStyle.Fill, PlaceholderText = "Ví dụ 106.699018" };
        var description = new TextBox { Text = source.Description, Multiline = true, Dock = DockStyle.Fill, MaxLength = 3000, ScrollBars = ScrollBars.Vertical };

        var labels = new[] { "Tên địa điểm *", "Khu vực du lịch *", "Loại hình *", "Địa chỉ mô tả *", "Thời lượng (phút) *", "Giờ mở cửa", "Giờ đóng cửa", "Vĩ độ", "Kinh độ", "Giới thiệu *" };
        Control[] fields = [name, region, category, address, minutes, open, close, latitude, longitude, description];
        for (var i = 0; i < fields.Length; i++)
        {
            layout.Controls.Add(Ui.Label(labels[i]), 0, i);
            layout.Controls.Add(fields[i], 1, i);
        }

        var help = Ui.Label("* Bắt buộc · Tích ô giờ để nhập giờ mở/đóng cửa · Nhập đủ vĩ độ và kinh độ hoặc để trống cả hai.", 9);
        layout.Controls.Add(help, 0, 10);
        layout.SetColumnSpan(help, 2);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = Ui.Button("Lưu bản nháp", true);
        var cancel = Ui.Button("Hủy");
        cancel.DialogResult = DialogResult.Cancel;
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 0, 11);
        layout.SetColumnSpan(actions, 2);
        Controls.Add(layout);
        AcceptButton = save;
        CancelButton = cancel;
        save.Text = item is null ? "Lưu bản nháp" : "Lưu thay đổi";
        save.Click += (_, _) =>
        {
            decimal? lat, lon;
            try
            {
                lat = Kit.ParseDecimal(latitude.Text);
                lon = Kit.ParseDecimal(longitude.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show(this, "Vĩ độ và kinh độ phải là số, ví dụ 10.779783.", "Kiểm tra thông tin");
                return;
            }

            var draft = new DestinationForm
            {
                Name = name.Text.Trim(),
                Region = region.Text,
                Category = category.Text,
                Address = address.Text.Trim(),
                VisitMinutes = (int)minutes.Value,
                OpenTime = open.Checked ? TimeOnly.FromDateTime(open.Value) : null,
                CloseTime = close.Checked ? TimeOnly.FromDateTime(close.Value) : null,
                Latitude = lat,
                Longitude = lon,
                Description = description.Text.Trim()
            };
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(draft, new ValidationContext(draft), errors, true))
            {
                MessageBox.Show(this, string.Join(Environment.NewLine, errors.Select(x => "• " + x.ErrorMessage).Distinct()), "Kiểm tra thông tin");
                return;
            }

            Value = draft;
            DialogResult = DialogResult.OK;
            Close();
        };
    }

    private static DateTimePicker TimePicker(TimeOnly? value, int defaultHour)
    {
        var picker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            ShowCheckBox = true,
            Width = 120,
            Anchor = AnchorStyles.Left,
            Value = DateTime.Today.AddHours(defaultHour)
        };
        if (value is { } time) picker.Value = DateTime.Today.Add(time.ToTimeSpan());
        picker.Checked = value is not null;
        return picker;
    }
}
