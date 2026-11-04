using DongNamTrip.Modules;

namespace DongNamTrip.Modules.Narration;

/// <summary>Lưu và đọc tệp audio WAV trên đĩa (<c>Storage:AudioRoot</c>, mặc định <c>App_Data/audio</c>). Chỉ nhận tên tệp, không nhận đường dẫn.</summary>
public sealed class AudioFileStore(IConfiguration configuration, IHostEnvironment environment)
{
    private string Root => configuration["Storage:AudioRoot"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "audio");

    public async Task<string> SaveAsync(int translationId, byte[] content, CancellationToken ct)
    {
        Directory.CreateDirectory(Root);
        var fileName = $"{translationId}-{Guid.NewGuid():N}.wav";
        await File.WriteAllBytesAsync(Path.Combine(Root, fileName), content, ct);
        return fileName;
    }

    /// <summary>Đường dẫn đầy đủ nếu tệp còn tồn tại; luôn bỏ phần thư mục của <paramref name="fileName"/> để không thoát khỏi thư mục audio.</summary>
    public string? Find(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        var path = Path.GetFullPath(Path.Combine(Root, Path.GetFileName(fileName)));
        return File.Exists(path) ? path : null;
    }

    public void Delete(string fileName)
    {
        if (Find(fileName) is { } path) File.Delete(path);
    }
}
