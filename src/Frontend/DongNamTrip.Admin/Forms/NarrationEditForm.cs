using System.ComponentModel.DataAnnotations;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

/// <summary>Soạn thuyết minh gốc, hoặc nội dung cho phiên bản mới (khi đó địa điểm và tiêu đề được khóa).</summary>
public sealed class NarrationEditForm : Form
{
    public NarrationForm Value { get; private set; } = new();

    public NarrationEditForm(NarrationInfo? item, IReadOnlyList<DestinationOption> places, bool newVersion, NarrationForm? initial = null)
    {
        Text = newVersion ? $"Phiên bản mới của \"{item?.Title}\"" : item is null ? "Soạn thuyết minh" : "Sửa thuyết minh";
        ClientSize = new Size(700, 600);
        MinimumSize = new Size(700, 600);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Ui.Background;
        var source = initial ?? (item is null ? new NarrationForm() : new NarrationForm { DestinationId = item.DestinationId, Title = item.Title, Source = item.Source });
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(25), ColumnCount = 2, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        var place = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(DestinationOption.Name), ValueMember = nameof(DestinationOption.Id), Enabled = item is null };
        place.DataSource = places.ToList();
        if (places.Any(x => x.Id == source.DestinationId)) place.SelectedValue = source.DestinationId;
        var title = new TextBox { Text = source.Title, Dock = DockStyle.Fill, MaxLength = 200, Enabled = !newVersion };
        var body = new TextBox { Text = source.Source, Multiline = true, Dock = DockStyle.Fill, MaxLength = 20000, ScrollBars = ScrollBars.Vertical };
        layout.Controls.Add(Ui.Label("Địa điểm *"), 0, 0);
        layout.Controls.Add(place, 1, 0);
        layout.Controls.Add(Ui.Label("Tiêu đề *"), 0, 1);
        layout.Controls.Add(title, 1, 1);
        layout.Controls.Add(Ui.Label("Nội dung gốc *"), 0, 2);
        layout.Controls.Add(body, 1, 2);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = Ui.Button("Lưu bản nháp", true);
        var cancel = Ui.Button("Hủy");
        cancel.DialogResult = DialogResult.Cancel;
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 0, 3);
        layout.SetColumnSpan(actions, 2);
        Controls.Add(layout);
        AcceptButton = null;
        CancelButton = cancel;
        save.Click += (_, _) =>
        {
            var draft = new NarrationForm { DestinationId = place.SelectedValue is int id ? id : 0, Title = title.Text.Trim(), Source = body.Text.Trim() };
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
