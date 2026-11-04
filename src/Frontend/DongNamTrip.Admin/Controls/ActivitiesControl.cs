using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

/// <summary>SCR15 – Hoạt động và suất (UC15): quản lý hoạt động theo địa điểm; tạo/sửa suất, công bố/tạm ngừng, cảnh báo hồ sơ bị ảnh hưởng.</summary>
public sealed class ActivitiesControl : UserControl
{
    private List<DestinationOption> places = [];
    private List<AdminActivity> activities = [];
    private readonly DataGridView activityGrid = Ui.Grid();
    private readonly DataGridView slotGrid = Ui.Grid();
    private readonly ComboBox placeFilter = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 10, 15, 5) };
    private readonly Label count = Ui.Label("");

    public ActivitiesControl(TripApiClient api, string token)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        var top = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var addActivity = Ui.Button("+ Thêm hoạt động", true);
        var editActivity = Ui.Button("Sửa hoạt động");
        var publishActivity = Ui.Button("Công bố");
        var pauseActivity = Ui.Button("Tạm ngừng");
        var refresh = Ui.Button("Tải lại");
        top.Controls.AddRange([placeFilter, addActivity, editActivity, publishActivity, pauseActivity, refresh]);
        var slotBar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var slotTitle = Ui.Label("Suất của hoạt động đã chọn", 10, true);
        slotTitle.Margin = new Padding(0, 14, 20, 5);
        var addSlot = Ui.Button("+ Thêm suất", true);
        var editSlot = Ui.Button("Sửa suất");
        var publishSlot = Ui.Button("Công bố suất");
        var pauseSlot = Ui.Button("Tạm ngừng suất");
        slotBar.Controls.AddRange([slotTitle, addSlot, editSlot, publishSlot, pauseSlot]);
        layout.Controls.Add(top);
        layout.Controls.Add(activityGrid);
        layout.Controls.Add(slotBar);
        layout.Controls.Add(slotGrid);
        layout.Controls.Add(count);
        Controls.Add(layout);

        int? FilterId() => placeFilter.SelectedValue is int id && id > 0 ? id : null;
        AdminActivity? CurrentActivity() => Kit.SelectedId(activityGrid) is { } id ? activities.FirstOrDefault(x => x.Id == id) : null;
        SlotInfo? CurrentSlot() => CurrentActivity() is { } a && Kit.SelectedId(slotGrid) is { } id ? a.Slots.FirstOrDefault(x => x.Id == id) : null;

        void ShowActivities()
        {
            var selected = Kit.SelectedId(activityGrid);
            activityGrid.DataSource = activities.Select(x => new
            {
                x.Id,
                Địa_điểm = x.DestinationName,
                Hoạt_động = x.Name,
                Thời_lượng_phút = x.DurationMinutes,
                Phí_tham_khảo = Kit.Money(x.ReferenceFee),
                Trạng_thái = Kit.Status(x.Status),
                Số_suất = x.Slots.Count
            }).ToList();
            if (activityGrid.Columns.Contains("Id")) activityGrid.Columns["Id"]!.Width = 55;
            Kit.SelectRow(activityGrid, selected);
            ShowSlots();
            count.Text = $"{activities.Count} hoạt động · Suất chỉ hiện cho khách khi hoạt động và địa điểm đều đã công bố.";
        }

        void ShowSlots()
        {
            var selected = Kit.SelectedId(slotGrid);
            slotGrid.DataSource = (CurrentActivity()?.Slots ?? []).Select(x => new
            {
                x.Id,
                Ngày = x.Date.ToString("dd/MM/yyyy"),
                Bắt_đầu = x.Start.ToString("HH:mm"),
                Kết_thúc = x.End.ToString("HH:mm"),
                Trạng_thái = Kit.Status(x.Status)
            }).ToList();
            if (slotGrid.Columns.Contains("Id")) slotGrid.Columns["Id"]!.Width = 55;
            Kit.SelectRow(slotGrid, selected);
        }

        async Task Reload()
        {
            if (IsDisposed) return;
            var keep = FilterId();
            places = await api.DestinationOptionsAsync(token);
            var options = new List<DestinationOption> { new(0, "Tất cả địa điểm", "") };
            options.AddRange(places);
            placeFilter.DisplayMember = nameof(DestinationOption.Name);
            placeFilter.ValueMember = nameof(DestinationOption.Id);
            placeFilter.DataSource = options;
            placeFilter.SelectedValue = keep ?? 0;
            await LoadActivities();
        }

        async Task LoadActivities()
        {
            activities = await api.AdminActivitiesAsync(FilterId(), token);
            if (!IsDisposed) ShowActivities();
        }

        async Task Edit(AdminActivity? item)
        {
            ActivityForm? draft = null;
            while (true)
            {
                using var dialog = new ActivityEditForm(item, places, FilterId(), draft);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                draft = dialog.Value;
                try
                {
                    var saved = await api.SaveActivityAsync(item?.Id, draft, token);
                    await LoadActivities();
                    Kit.SelectRow(activityGrid, saved.Id);
                    ShowSlots();
                    return;
                }
                catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
                {
                    Kit.Warn(this, Kit.Describe(error), "Chưa lưu được");
                    if (error is HttpRequestException { StatusCode: null } or TaskCanceledException) return;
                }
            }
        }

        async Task EditSlot(AdminActivity activity, SlotInfo? item)
        {
            SlotForm? draft = null;
            while (true)
            {
                using var dialog = new SlotEditForm(activity.Name, item, draft);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                draft = dialog.Value;
                try
                {
                    await api.SaveSlotAsync(activity.Id, item?.Id, draft, token);
                    await LoadActivities();
                    Kit.SelectRow(activityGrid, activity.Id);
                    ShowSlots();
                    return;
                }
                catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
                {
                    Kit.Warn(this, Kit.Describe(error), "Chưa lưu được");
                    if (error is HttpRequestException { StatusCode: null } or TaskCanceledException) return;
                }
            }
        }

        // Đổi trạng thái: nếu có lịch trình của khách đang dùng, API trả NeedConfirm để hiện cảnh báo trước khi áp dụng.
        async Task Switch(Func<bool, Task<ImpactResult>> call)
        {
            var result = await call(false);
            if (result.Status == "NeedConfirm")
            {
                if (!Kit.Confirm(this, result.Message, "Cảnh báo hồ sơ bị ảnh hưởng")) return;
                result = await call(true);
            }

            if (result.AffectedItineraries > 0) MessageBox.Show(this, result.Message, "Đã cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);
            var selected = CurrentActivity()?.Id;
            await LoadActivities();
            Kit.SelectRow(activityGrid, selected);
            ShowSlots();
        }

        placeFilter.SelectionChangeCommitted += async (_, _) => await Ui.RunAsync(this, LoadActivities);
        activityGrid.SelectionChanged += (_, _) => ShowSlots();
        refresh.Click += async (_, _) => await Ui.RunAsync(this, Reload);
        addActivity.Click += async (_, _) => await SafeAsync(() => Edit(null));
        editActivity.Click += async (_, _) =>
        {
            if (CurrentActivity() is not { } item) { MessageBox.Show(this, "Chọn một hoạt động trước."); return; }
            await SafeAsync(() => Edit(item));
        };
        publishActivity.Click += async (_, _) =>
        {
            if (CurrentActivity() is not { } item) { MessageBox.Show(this, "Chọn một hoạt động trước."); return; }
            await Ui.RunAsync(this, () => Switch(confirm => api.SetActivityStatusAsync(item.Id, ContentStatus.Published, confirm, token)));
        };
        pauseActivity.Click += async (_, _) =>
        {
            if (CurrentActivity() is not { } item) { MessageBox.Show(this, "Chọn một hoạt động trước."); return; }
            await Ui.RunAsync(this, () => Switch(confirm => api.SetActivityStatusAsync(item.Id, ContentStatus.Inactive, confirm, token)));
        };
        addSlot.Click += async (_, _) =>
        {
            if (CurrentActivity() is not { } activity) { MessageBox.Show(this, "Chọn một hoạt động trước khi thêm suất."); return; }
            await SafeAsync(() => EditSlot(activity, null));
        };
        editSlot.Click += async (_, _) =>
        {
            if (CurrentActivity() is not { } activity || CurrentSlot() is not { } slot) { MessageBox.Show(this, "Chọn một suất trước."); return; }
            await SafeAsync(() => EditSlot(activity, slot));
        };
        publishSlot.Click += async (_, _) =>
        {
            if (CurrentSlot() is not { } slot) { MessageBox.Show(this, "Chọn một suất trước."); return; }
            await Ui.RunAsync(this, () => Switch(confirm => api.SetSlotStatusAsync(slot.Id, ContentStatus.Published, confirm, token)));
        };
        pauseSlot.Click += async (_, _) =>
        {
            if (CurrentSlot() is not { } slot) { MessageBox.Show(this, "Chọn một suất trước."); return; }
            await Ui.RunAsync(this, () => Switch(confirm => api.SetSlotStatusAsync(slot.Id, ContentStatus.Inactive, confirm, token)));
        };
        Load += async (_, _) => await Ui.RunAsync(this, Reload);
    }

    private async Task SafeAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { Kit.Warn(this, Kit.Describe(error)); }
    }
}
