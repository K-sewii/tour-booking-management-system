namespace DongNamTrip.Admin;

/// <summary>Mô tả một ô nhập của <see cref="FieldsDialog"/>: nhãn, giá trị đầu, nhiều dòng, hoặc danh sách chọn.</summary>
internal sealed record Field(string Label, string Value = "", bool Multiline = false, string[]? Options = null);

/// <summary>Hộp thoại nhập nhiều trường văn bản dùng cho tour, lịch khởi hành, tiền, điều chỉnh đơn, tài khoản nhân viên.</summary>
internal sealed class FieldsDialog : Form
{
    private readonly List<Control> inputs = [];
    private readonly Func<string[], string?>? validate;

    public FieldsDialog(string title, IReadOnlyList<Field> fields, Func<string[], string?>? validate = null)
    {
        this.validate = validate;
        Text = title; Size = new Size(520, Math.Min(760, 190 + fields.Sum(field => field.Multiline ? 105 : 62)));
        StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(20) };
        foreach (var field in fields)
        {
            Control input;
            if (field.Options is { Length: > 0 })
            {
                var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 450 };
                combo.Items.AddRange(field.Options.Cast<object>().ToArray());
                combo.SelectedItem = field.Options.Contains(field.Value) ? field.Value : field.Options[0];
                input = combo;
            }
            else input = new TextBox { Text = field.Value, Multiline = field.Multiline, Height = field.Multiline ? 80 : 28, Width = 450 };
            inputs.Add(input);
            layout.Controls.Add(Ui.Label(field.Label));
            layout.Controls.Add(input);
        }
        var ok = Ui.Button("Lưu", true);
        ok.Click += (_, _) =>
        {
            if (this.validate?.Invoke(Values) is { } problem) { MessageBox.Show(this, problem, "Chưa hợp lệ"); return; }
            DialogResult = DialogResult.OK;
        };
        layout.Controls.Add(ok);
        Controls.Add(layout);
    }

    public string[] Values => inputs.Select(input => input is ComboBox combo ? Convert.ToString(combo.SelectedItem) ?? "" : input.Text.Trim()).ToArray();
}
