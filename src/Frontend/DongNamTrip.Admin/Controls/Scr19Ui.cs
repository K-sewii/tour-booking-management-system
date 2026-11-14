namespace DongNamTrip.Admin;

internal static class Scr19Ui
{
    internal static TableLayoutPanel Cards(Label[] values, params string[] captions)
    {
        var cards = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = values.Length, Margin = new Padding(0, 8, 0, 8) };
        var panels = new List<Control>();
        for (var i = 0; i < values.Length; i++)
        {
            var card = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.White, Padding = new Padding(14, 8, 14, 8), Margin = new Padding(0, 0, 10, 0) };
            card.Controls.Add(Ui.Label(captions[i], 9)); card.Controls.Add(values[i]); panels.Add(card); cards.Controls.Add(card);
        }
        var columns = 0;
        void Arrange()
        {
            var desired = values.Length >= 4 && cards.ClientSize.Width < values.Length * 210 * cards.DeviceDpi / 96 ? (values.Length + 1) / 2 : values.Length;
            if (desired == columns) return;
            columns = desired; cards.SuspendLayout(); cards.ColumnStyles.Clear(); cards.RowStyles.Clear();
            cards.ColumnCount = columns; cards.RowCount = (values.Length + columns - 1) / columns;
            for (var i = 0; i < columns; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
            for (var i = 0; i < cards.RowCount; i++) cards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            for (var i = 0; i < panels.Count; i++) cards.SetCellPosition(panels[i], new TableLayoutPanelCellPosition(i % columns, i / columns));
            cards.ResumeLayout(true);
        }
        cards.SizeChanged += (_, _) => Arrange();
        Arrange();
        return cards;
    }
    internal static TableLayoutPanel CreateLayout()
    {
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        p.RowStyles.Add(new RowStyle(SizeType.AutoSize)); p.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); p.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        return p;
    }
    internal static FlowLayoutPanel Bar(params Control[] controls)
    {
        var p = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        p.Controls.AddRange(controls); return p;
    }
    internal static void StyleGrid(DataGridView grid)
    {
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Color.FromArgb(232, 237, 234);
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ShowCellToolTips = true;
        grid.DataBindingComplete += (_, _) => { foreach (DataGridViewColumn column in grid.Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable; };
        grid.CellFormatting += (_, e) => { if (e.RowIndex >= 0 && e.ColumnIndex >= 0) grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = Convert.ToString(e.Value) ?? ""; };
    }
}
