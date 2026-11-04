using System.Speech.Synthesis;

namespace DongNamTrip.Admin;

/// <summary>Tạo giọng đọc bằng bộ TTS có sẵn của Windows, xuất ra tệp WAV (UC22).</summary>
internal static class Tts
{
    public static List<string> VoicesFor(string languageCode)
    {
        using var synthesizer = new SpeechSynthesizer();
        return synthesizer.GetInstalledVoices()
            .Where(v => v.Enabled && string.Equals(v.VoiceInfo.Culture.TwoLetterISOLanguageName, languageCode, StringComparison.OrdinalIgnoreCase))
            .Select(v => v.VoiceInfo.Name).ToList();
    }

    public static byte[] Synthesize(string voice, string text)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dnt-{Guid.NewGuid():N}.wav");
        try
        {
            using (var synthesizer = new SpeechSynthesizer())
            {
                synthesizer.SelectVoice(voice);
                synthesizer.SetOutputToWaveFile(path);
                synthesizer.Speak(text);
            }

            return File.ReadAllBytes(path);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
