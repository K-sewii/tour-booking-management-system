using System.ComponentModel.DataAnnotations;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;
public sealed class ActivityEditForm : Form
{
    public ActivityForm Value { get; private set; } = new();

    public ActivityEditForm(AdminActivity? item, IReadOnlyList<DestinationOption> places, int? defaultDestinationId, ActivityForm? initial = null)
    {
        Text = item is null ? "Thêm hoạt động" : "Sửa hoạt động";
        ClientSize = new Size(620, 600);
        MinimumSize = new Size(620, 600);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Ui.Background;
        var source = initial ?? (item is null ? new ActivityForm { DestinationId = defaultDestinationId ?? 0 } : new ActivityForm
        {
            DestinationId = item.DestinationId,
            Name = item.Name,
            Description = item.Description,
            DurationMinutes = item.DurationMinutes,
            Conditions = item.Conditions,
            ReferenceFee = item.ReferenceFee
        });
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(25), ColumnCount = 2, RowCount = 8 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));

        var place = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(DestinationOption.Name), ValueMember = nameof(DestinationOption.Id) };
        place.DataSource = places.ToList();
        if (places.Any(x => x.Id == source.DestinationId)) place.SelectedValue = source.DestinationId;
        var name = new TextBox { Text = source.Name, Dock = DockStyle.Fill, MaxLength = 150 };
        var minutes = new NumericUpDown { Minimum = 1, Maximum = 1440, Increment = 15, Value = Math.Clamp(source.DurationMinutes, 1, 1440), Dock = DockStyle.Fill };
        var fee = new TextBox { Text = source.ReferenceFee?.ToString("0") ?? "", Dock = DockStyle.Fill, PlaceholderText = "Để trống nếu chưa có · 0 = miễn phí" };
        var description = new TextBox { Text = source.Description ?? "", Multiline = true, Dock = DockStyle.Fill, MaxLength = 2000, ScrollBars = ScrollBars.Vertical };
        var conditions = new TextBox { Text = source.Conditions ?? "", Multiline = true, Dock = DockStyle.Fill, MaxLength = 500, ScrollBars = ScrollBars.Vertical };
        var labels = new[] { "Địa điểm *", "Tên hoạt động *", "Thời lượng (phút) *", "Phí tham khảo (₫)", "Mô tả", "Điều kiện tham gia" };
        Control[] fields = [place, name, minutes, fee, description, conditions];
        for (var i = 0; i < fields.Length; i++)
        {
            layout.Controls.Add(Ui.Label(labels[i]), 0, i);
            layout.Controls.Add(fields[i], 1, i);
        }

        var help = Ui.Label("Hoạt động mới ở trạng thái Bản nháp; công bố sau khi kiểm tra.", 9);
        layout.Controls.Add(help, 0, 6);
        layout.SetColumnSpan(help, 2);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = Ui.Button("Lưu hoạt động", true);
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
            decimal? parsedFee;
            try { parsedFee = Kit.ParseMoney(fee.Text); }
            catch (FormatException)
            {
                MessageBox.Show(this, "Phí tham khảo phải là số tiền nguyên (₫), ví dụ 150000 hoặc 150.000.", "Kiểm tra thông tin");
                return;
            }

            var draft = new ActivityForm
            {
                DestinationId = place.SelectedValue is int id ? id : 0,
                Name = name.Text.Trim(),
                Description = description.Text.Trim(),
                DurationMinutes = (int)minutes.Value,
                Conditions = conditions.Text.Trim(),
                ReferenceFee = parsedFee
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
}
