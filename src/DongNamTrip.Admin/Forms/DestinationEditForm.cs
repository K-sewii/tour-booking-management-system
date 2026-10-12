using System.ComponentModel.DataAnnotations;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;
public sealed class DestinationEditForm : Form
{
    public DestinationDraft Value { get; private set; } = new();

    public DestinationEditForm(Destination? item)
    {
        Text = item is null ? "Thêm địa điểm" : "Sửa địa điểm";
        ClientSize = new Size(610, 645);
        MinimumSize = new Size(610, 645);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Ui.Background;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(25),
            ColumnCount = 2,
            RowCount = 8
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 5; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        var name = new TextBox
        {
            Text = item?.Name ?? "",
            Dock = DockStyle.Fill,
            MaxLength = 120
        };
        var region = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        region.Items.AddRange(CatalogOptions.Regions);
        region.SelectedItem = item?.Region ?? CatalogOptions.Regions[0];
        var category = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        category.Items.AddRange(CatalogOptions.Categories);
        category.SelectedItem = item?.Category ?? CatalogOptions.Categories[0];
        var address = new TextBox
        {
            Text = item?.Address ?? "",
            Dock = DockStyle.Fill,
            MaxLength = 250
        };
        var minutes = new NumericUpDown
        {
            Minimum = 15,
            Maximum = 1440,
            Increment = 15,
            Value = item?.VisitMinutes ?? 90,
            Dock = DockStyle.Fill
        };
        var description = new TextBox
        {
            Text = item?.Description ?? "",
            Multiline = true,
            Dock = DockStyle.Fill,
            MaxLength = 3000,
            ScrollBars = ScrollBars.Vertical
        };
        var labels = new[]
        {
            "Tên địa điểm *",
            "Khu vực du lịch *",
            "Loại hình *",
            "Địa chỉ mô tả *",
            "Thời lượng (phút)",
            "Giới thiệu *"
        };
        Control[] fields = [name, region, category, address, minutes, description];
        for (var i = 0; i < fields.Length; i++)
        {
            layout.Controls.Add(Ui.Label(labels[i]), 0, i);
            layout.Controls.Add(fields[i], 1, i);
        }

        var help = Ui.Label("* Bắt buộc · Hình minh họa tự chọn theo khu vực.", 9);
        layout.Controls.Add(help, 0, 6);
        layout.SetColumnSpan(help, 2);
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var save = Ui.Button("Lưu địa điểm", true);
        var cancel = Ui.Button("Hủy");
        cancel.DialogResult = DialogResult.Cancel;
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 0, 7);
        layout.SetColumnSpan(actions, 2);
        Controls.Add(layout);
        AcceptButton = save;
        CancelButton = cancel;
        save.Click += (_, _) =>
        {
            var draft = new DestinationDraft
            {
                Name = name.Text.Trim(),
                Region = region.Text,
                Category = category.Text,
                Address = address.Text.Trim(),
                VisitMinutes = (int)minutes.Value,
                Description = description.Text.Trim()
            };
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(draft, new ValidationContext(draft), errors, true))
            {
                MessageBox.Show(this, "Vui lòng điền đầy đủ tên, địa chỉ và giới thiệu; kiểm tra độ dài các trường.", "Kiểm tra thông tin");
                return;
            }

            Value = draft;
            DialogResult = DialogResult.OK;
            Close();
        };
    }
}
