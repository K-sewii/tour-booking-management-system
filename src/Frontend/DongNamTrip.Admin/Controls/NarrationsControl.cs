using System.Media;
using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Admin;

/// <summary>SCR20 – Biên tập thuyết minh và audio (UC21, UC22): soạn, phiên bản, dịch, duyệt, TTS, nghe thử, công bố.</summary>
public sealed class NarrationsControl : UserControl
{
    private List<NarrationInfo> narrations = [];
    private List<DestinationOption> places = [];
    private List<LanguageInfo> languages = [];
    private readonly DataGridView narrationGrid = Ui.Grid();
    private readonly DataGridView translationGrid = Ui.Grid();
    private readonly DataGridView audioGrid = Ui.Grid();
    private readonly ComboBox voice = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 10, 12, 5) };
    private readonly Label hint = Ui.Label("");
    private SoundPlayer? player;

    public NarrationsControl(TripApiClient api, string token, IReadOnlyCollection<string> roles)
    {
        var canEdit = roles.Contains(AppRoles.Admin) || roles.Contains(AppRoles.Editor);
        var canPublish = roles.Contains(AppRoles.Admin) || roles.Contains(AppRoles.Publisher);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 31));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 31));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        var bar1 = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var create = Ui.Button("+ Soạn thuyết minh", true);
        var edit = Ui.Button("Sửa nội dung");
        var version = Ui.Button("Phiên bản mới");
        var submit = Ui.Button("Gửi duyệt");
        var approve = Ui.Button("Duyệt");
        var reject = Ui.Button("Trả về nháp");
        var publish = Ui.Button("Công bố");
        var unpublish = Ui.Button("Ngừng công bố");
        var refresh = Ui.Button("Tải lại");
        bar1.Controls.AddRange([create, edit, version, submit, approve, reject, publish, unpublish, refresh]);
        var bar2 = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var title2 = Ui.Label("Bản dịch", 10, true);
        title2.Margin = new Padding(0, 14, 15, 5);
        var translate = Ui.Button("Thêm/sửa bản dịch", true);
        var tSubmit = Ui.Button("Gửi duyệt");
        var tApprove = Ui.Button("Duyệt");
        var tReject = Ui.Button("Trả về nháp");
        var tStop = Ui.Button("Ngừng công bố");
        bar2.Controls.AddRange([title2, translate, tSubmit, tApprove, tReject, tStop]);
        var bar3 = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var title3 = Ui.Label("Audio", 10, true);
        title3.Margin = new Padding(0, 14, 15, 5);
        var generate = Ui.Button("Tạo audio (TTS)", true);
        var listen = Ui.Button("Nghe thử");
        var stopListen = Ui.Button("Dừng");
        var aPublish = Ui.Button("Công bố audio");
        var aStop = Ui.Button("Ngừng công bố audio");
        bar3.Controls.AddRange([title3, voice, generate, listen, stopListen, aPublish, aStop]);
        foreach (var control in new Control[] { bar1, narrationGrid, bar2, translationGrid, bar3, audioGrid, hint }) layout.Controls.Add(control);
        Controls.Add(layout);

        foreach (var button in new[] { create, edit, version, submit, translate, tSubmit, generate })
            button.Enabled = canEdit;
        foreach (var button in new[] { approve, publish, unpublish, tApprove, tStop, aPublish, aStop })
            button.Enabled = canPublish;
        reject.Enabled = tReject.Enabled = canEdit || canPublish;
        hint.Text = canEdit && !canPublish ? "Bạn soạn, dịch và tạo audio; việc duyệt và công bố do người xuất bản thực hiện."
            : canPublish && !canEdit ? "Bạn duyệt và công bố nội dung do biên tập viên gửi lên." : "";

        NarrationInfo? CurrentNarration() => Kit.SelectedId(narrationGrid) is { } id ? narrations.FirstOrDefault(x => x.Id == id) : null;
        TranslationInfo? CurrentTranslation() => CurrentNarration() is { } n && Kit.SelectedId(translationGrid) is { } id ? n.Translations.FirstOrDefault(x => x.Id == id) : null;
        AudioInfo? CurrentAudio() => CurrentTranslation() is { } t && Kit.SelectedId(audioGrid) is { } id ? t.Audios.FirstOrDefault(x => x.Id == id) : null;

        void ShowNarrations()
        {
            var selected = Kit.SelectedId(narrationGrid);
            narrationGrid.DataSource = narrations.Select(x => new
            {
                x.Id,
                Địa_điểm = x.DestinationName,
                Tiêu_đề = x.Title,
                Phiên_bản = x.Version,
                Trạng_thái = Kit.Status(x.Status),
                Bản_dịch = x.Translations.Count
            }).ToList();
            if (narrationGrid.Columns.Contains("Id")) narrationGrid.Columns["Id"]!.Width = 55;
            Kit.SelectRow(narrationGrid, selected);
            ShowTranslations();
        }

        void ShowTranslations()
        {
            var selected = Kit.SelectedId(translationGrid);
            translationGrid.DataSource = (CurrentNarration()?.Translations ?? []).Select(x => new
            {
                x.Id,
                Ngôn_ngữ = x.LanguageName,
                Trạng_thái = Kit.Status(x.Status),
                Số_audio = x.Audios.Count,
                Nội_dung = x.Content.Length > 90 ? x.Content[..90] + "…" : x.Content
            }).ToList();
            if (translationGrid.Columns.Contains("Id")) translationGrid.Columns["Id"]!.Width = 55;
            Kit.SelectRow(translationGrid, selected);
            ShowAudio();
        }

        void ShowAudio()
        {
            var selected = Kit.SelectedId(audioGrid);
            audioGrid.DataSource = (CurrentTranslation()?.Audios ?? []).Select(x => new
            {
                x.Id,
                Giọng_đọc = x.Voice,
                Trạng_thái = Kit.Status(x.Status),
                Ngày_công_bố = x.PublishedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "—"
            }).ToList();
            if (audioGrid.Columns.Contains("Id")) audioGrid.Columns["Id"]!.Width = 55;
            Kit.SelectRow(audioGrid, selected);
            RefreshVoices();
        }

        void RefreshVoices()
        {
            voice.DataSource = null;
            if (CurrentTranslation() is not { } translation) return;
            try
            {
                var voices = Tts.VoicesFor(translation.LanguageCode);
                voice.DataSource = voices;
            }
            catch (Exception)
            {
                voice.DataSource = null;
            }
        }

        async Task Reload()
        {
            if (IsDisposed) return;
            var n = Kit.SelectedId(narrationGrid);
            var t = Kit.SelectedId(translationGrid);
            places = await api.DestinationOptionsAsync(token);
            languages = await api.LanguagesAsync(token);
            narrations = await api.NarrationsAsync(token);
            if (IsDisposed) return;
            ShowNarrations();
            Kit.SelectRow(narrationGrid, n);
            ShowTranslations();
            Kit.SelectRow(translationGrid, t);
        }

        async Task Run(Func<Task> action) => await Ui.RunAsync(this, async () =>
        {
            await action();
            await Reload();
        });

        async Task OpenNarration(NarrationInfo? item, bool newVersion)
        {
            NarrationForm? draft = null;
            while (true)
            {
                using var dialog = new NarrationEditForm(item, places, newVersion, draft);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                draft = dialog.Value;
                try
                {
                    if (newVersion && item is not null) await api.NewNarrationVersionAsync(item.Id, new NewVersionForm { Source = draft.Source }, token);
                    else await api.SaveNarrationAsync(item?.Id, draft, token);
                    await Reload();
                    return;
                }
                catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
                {
                    Kit.Warn(this, Kit.Describe(error), "Chưa lưu được");
                    if (error is HttpRequestException { StatusCode: null } or TaskCanceledException) return;
                }
            }
        }

        async Task OpenTranslation(NarrationInfo narration)
        {
            TranslationForm? draft = null;
            while (true)
            {
                using var dialog = new TranslationEditForm(narration, languages, draft);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                draft = dialog.Value;
                try
                {
                    await api.SaveTranslationAsync(narration.Id, draft, token);
                    await Reload();
                    return;
                }
                catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
                {
                    Kit.Warn(this, Kit.Describe(error), "Chưa lưu được");
                    if (error is HttpRequestException { StatusCode: null } or TaskCanceledException) return;
                }
            }
        }

        bool NeedNarration(out NarrationInfo narration)
        {
            narration = CurrentNarration()!;
            if (narration is not null) return true;
            MessageBox.Show(this, "Chọn một thuyết minh trước.");
            return false;
        }

        bool NeedTranslation(out TranslationInfo translation)
        {
            translation = CurrentTranslation()!;
            if (translation is not null) return true;
            MessageBox.Show(this, "Chọn một bản dịch trước.");
            return false;
        }

        bool NeedAudio(out AudioInfo audio)
        {
            audio = CurrentAudio()!;
            if (audio is not null) return true;
            MessageBox.Show(this, "Chọn một tệp audio trước.");
            return false;
        }

        narrationGrid.SelectionChanged += (_, _) => ShowTranslations();
        translationGrid.SelectionChanged += (_, _) => ShowAudio();
        refresh.Click += async (_, _) => await Ui.RunAsync(this, Reload);
        create.Click += async (_, _) => await SafeAsync(() => OpenNarration(null, false));
        edit.Click += async (_, _) => { if (NeedNarration(out var n)) await SafeAsync(() => OpenNarration(n, false)); };
        version.Click += async (_, _) => { if (NeedNarration(out var n)) await SafeAsync(() => OpenNarration(n, true)); };
        submit.Click += async (_, _) => { if (NeedNarration(out var n)) await Run(() => api.SetNarrationStatusAsync(n.Id, ContentStatus.Pending, token)); };
        approve.Click += async (_, _) => { if (NeedNarration(out var n)) await Run(() => api.SetNarrationStatusAsync(n.Id, ContentStatus.Approved, token)); };
        reject.Click += async (_, _) => { if (NeedNarration(out var n)) await Run(() => api.SetNarrationStatusAsync(n.Id, ContentStatus.Draft, token)); };
        publish.Click += async (_, _) =>
        {
            if (NeedNarration(out var n) && Kit.Confirm(this, $"Công bố thuyết minh \"{n.Title}\" (v{n.Version})? Các phiên bản đang công bố trước đó sẽ ngừng."))
                await Run(() => api.SetNarrationStatusAsync(n.Id, ContentStatus.Published, token));
        };
        unpublish.Click += async (_, _) =>
        {
            if (NeedNarration(out var n) && Kit.Confirm(this, $"Ngừng công bố thuyết minh \"{n.Title}\" (v{n.Version})? Khách sẽ không nghe được nữa."))
                await Run(() => api.SetNarrationStatusAsync(n.Id, ContentStatus.Inactive, token));
        };
        translate.Click += async (_, _) => { if (NeedNarration(out var n)) await SafeAsync(() => OpenTranslation(n)); };
        tSubmit.Click += async (_, _) => { if (NeedTranslation(out var t)) await Run(() => api.SetTranslationStatusAsync(t.Id, ContentStatus.Pending, token)); };
        tApprove.Click += async (_, _) => { if (NeedTranslation(out var t)) await Run(() => api.SetTranslationStatusAsync(t.Id, ContentStatus.Approved, token)); };
        tReject.Click += async (_, _) => { if (NeedTranslation(out var t)) await Run(() => api.SetTranslationStatusAsync(t.Id, ContentStatus.Draft, token)); };
        tStop.Click += async (_, _) =>
        {
            if (NeedTranslation(out var t) && Kit.Confirm(this, $"Ngừng công bố bản dịch {t.LanguageName}? Audio đang công bố cũng ngừng."))
                await Run(() => api.SetTranslationStatusAsync(t.Id, ContentStatus.Inactive, token));
        };
        generate.Click += async (_, _) =>
        {
            if (!NeedTranslation(out var t)) return;
            if (voice.SelectedItem is not string selectedVoice)
            {
                Kit.Warn(this, $"Máy này chưa có giọng đọc cho {t.LanguageName}. Hãy cài gói giọng nói của Windows cho ngôn ngữ này rồi thử lại.", "Chưa có giọng đọc");
                return;
            }

            await Run(async () =>
            {
                byte[]? wav = null;
                string? failure = null;
                try { wav = await Task.Run(() => Tts.Synthesize(selectedVoice, t.Content)); }
                catch (Exception error) { failure = error.Message; }
                await api.UploadAudioAsync(t.Id, new AudioUpload { Voice = selectedVoice, WavBase64 = wav is null ? null : Convert.ToBase64String(wav), Error = failure }, token);
                if (failure is not null) Kit.Warn(this, "Tạo giọng đọc thất bại, hệ thống đã ghi nhận trạng thái Lỗi để bạn thử lại:\n" + failure, "TTS lỗi");
            });
        };
        listen.Click += async (_, _) =>
        {
            if (!NeedAudio(out var a)) return;
            await Ui.RunAsync(this, async () =>
            {
                var bytes = await api.AudioBytesAsync(a.Id, token);
                player?.Stop();
                player?.Dispose();
                player = new SoundPlayer(new MemoryStream(bytes));
                player.Play();
            });
        };
        stopListen.Click += (_, _) => player?.Stop();
        aPublish.Click += async (_, _) =>
        {
            if (NeedAudio(out var a) && Kit.Confirm(this, "Công bố audio này cho khách? Audio đang công bố khác của bản dịch sẽ ngừng."))
                await Run(() => api.SetAudioStatusAsync(a.Id, ContentStatus.Published, token));
        };
        aStop.Click += async (_, _) =>
        {
            if (NeedAudio(out var a) && Kit.Confirm(this, "Ngừng công bố audio này?")) await Run(() => api.SetAudioStatusAsync(a.Id, ContentStatus.Inactive, token));
        };
        Disposed += (_, _) => { player?.Stop(); player?.Dispose(); };
        Load += async (_, _) => await Ui.RunAsync(this, Reload);
    }

    private async Task SafeAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { Kit.Warn(this, Kit.Describe(error)); }
    }
}
