using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

public sealed class TranslationEditForm : Form
{
    public TranslationForm Value { get; private set; } = new();

    public TranslationEditForm(NarrationInfo narration, IReadOnlyList<LanguageInfo> languages, TranslationForm? initial = null)
    {
        Text = $"Bản dịch · {narration.Title} (v{narration.Version})";
        ClientSize = new Size(720, 640);
        MinimumSize = new Size(720, 640);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Ui.Background;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(25), ColumnCount = 2, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        var language = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(LanguageInfo.Name), ValueMember = nameof(LanguageInfo.Code) };
        language.DataSource = languages.ToList();
        var original = new TextBox { Text = narration.Source, Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
        var content = new TextBox { Multiline = true, Dock = DockStyle.Fill, MaxLength = 20000, ScrollBars = ScrollBars.Vertical, Text = initial?.Content ?? "" };
        if (initial is not null && languages.Any(x => x.Code == initial.LanguageCode)) language.SelectedValue = initial.LanguageCode;
        language.SelectedIndexChanged += (_, _) =>
        {
            // Chọn ngôn ngữ đã có bản dịch thì nạp nội dung hiện tại để sửa.
            if (language.SelectedValue is string code && narration.Translations.FirstOrDefault(x => x.LanguageCode == code) is { } existing && content.Text.Length == 0)
                content.Text = existing.Content;
        };
        layout.Controls.Add(Ui.Label("Ngôn ngữ *"), 0, 0);
        layout.Controls.Add(language, 1, 0);
        layout.Controls.Add(Ui.Label("Nội dung gốc"), 0, 1);
        layout.Controls.Add(original, 1, 1);
        layout.Controls.Add(Ui.Label("Bản dịch *"), 0, 2);
        layout.Controls.Add(content, 1, 2);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = Ui.Button("Lưu bản dịch", true);
        var cancel = Ui.Button("Hủy");
        cancel.DialogResult = DialogResult.Cancel;
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 0, 3);
        layout.SetColumnSpan(actions, 2);
        Controls.Add(layout);
        CancelButton = cancel;
        if (content.Text.Length == 0 && language.SelectedValue is string first && narration.Translations.FirstOrDefault(x => x.LanguageCode == first) is { } current)
            content.Text = current.Content;
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(content.Text))
            {
                MessageBox.Show(this, "Vui lòng nhập nội dung bản dịch.", "Kiểm tra thông tin");
                return;
            }

            Value = new TranslationForm { LanguageCode = language.SelectedValue as string ?? "vi", Content = content.Text.Trim() };
            DialogResult = DialogResult.OK;
            Close();
        };
    }
}
