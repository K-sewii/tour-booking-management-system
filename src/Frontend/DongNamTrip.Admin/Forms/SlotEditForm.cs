using System.ComponentModel.DataAnnotations;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;
public sealed class SlotEditForm : Form
{
    public SlotForm Value { get; private set; } = new();

    public SlotEditForm(string activityName, SlotInfo? item, SlotForm? initial = null)
    {
        Text = item is null ? "Thêm suất" : "Sửa suất";
        ClientSize = new Size(480, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Ui.Background;
        var source = initial ?? (item is null ? new SlotForm() : new SlotForm { Date = item.Date, Start = item.Start, End = item.End });
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(25), ColumnCount = 2, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var title = Ui.Label(activityName, 10, true);
        layout.Controls.Add(title, 0, 0);
        layout.SetColumnSpan(title, 2);
        var date = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = source.Date.ToDateTime(TimeOnly.MinValue), Dock = DockStyle.Fill, MinDate = DateTime.Today.AddYears(-1) };
        var start = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Value = DateTime.Today.Add(source.Start.ToTimeSpan()), Dock = DockStyle.Fill };
        var end = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Value = DateTime.Today.Add(source.End.ToTimeSpan()), Dock = DockStyle.Fill };
        var labels = new[] { "Ngày *", "Bắt đầu *", "Kết thúc *" };
        Control[] fields = [date, start, end];
        for (var i = 0; i < fields.Length; i++)
        {
            layout.Controls.Add(Ui.Label(labels[i]), 0, i + 1);
            layout.Controls.Add(fields[i], 1, i + 1);
        }

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = Ui.Button("Lưu suất", true);
        var cancel = Ui.Button("Hủy");
        cancel.DialogResult = DialogResult.Cancel;
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 0, 4);
        layout.SetColumnSpan(actions, 2);
        Controls.Add(layout);
        AcceptButton = save;
        CancelButton = cancel;
        save.Click += (_, _) =>
        {
            var draft = new SlotForm { Date = DateOnly.FromDateTime(date.Value), Start = TimeOnly.FromDateTime(start.Value), End = TimeOnly.FromDateTime(end.Value) };
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(draft, new ValidationContext(draft), errors, true))
            {
                MessageBox.Show(this, string.Join(Environment.NewLine, errors.Select(x => "• " + x.ErrorMessage)), "Kiểm tra thông tin");
                return;
            }

            if (draft.Date < DateOnly.FromDateTime(DateTime.Today))
            {
                MessageBox.Show(this, "Ngày tổ chức không được ở quá khứ.", "Kiểm tra thông tin");
                return;
            }

            Value = draft;
            DialogResult = DialogResult.OK;
            Close();
        };
    }
}
