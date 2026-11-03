using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Narration;

public sealed record NarrationHead(int Id, int DestinationId, string Title, int Version, string Status, int TranslationCount);

public sealed record TranslationHead(int Id, int NarrationId, string Status);

public sealed record AudioHead(int Id, int TranslationId, string TranslationStatus, int NarrationId, string NarrationStatus, string Status, string FileName);

/// <summary>Thuyết minh, bản dịch và tệp audio (SCR20). Không chứa quy tắc nghiệp vụ.</summary>
public sealed class NarrationRepository(DongNamTripDbContext db)
{
    private IQueryable<Thuyetminh> Full => db.Thuyetminh.AsNoTracking()
        .Include(x => x.MaDdNavigation)
        .Include(x => x.Bandich).ThenInclude(x => x.MaNnNavigation)
        .Include(x => x.Bandich).ThenInclude(x => x.Tepaudio);

    private static NarrationInfo ToDto(Thuyetminh t) => new(t.MaTm, t.MaDd, t.MaDdNavigation.TenDd, t.TieuDe, t.NoiDungGoc, t.PhienBan, t.TrangThai,
        t.Bandich.OrderBy(b => b.MaNn).Select(b => new TranslationInfo(b.MaBd, b.MaTm, b.MaNn, b.MaNnNavigation.TenNn, b.NoiDungDich, b.TrangThai,
            b.Tepaudio.OrderByDescending(a => a.MaTep).Select(a => new AudioInfo(a.MaTep, a.MaBd, a.GiongDoc, a.DuongDan, a.TrangThai, a.NgayCongBo)).ToList())).ToList());

    public Task<List<LanguageInfo>> LanguagesAsync(CancellationToken ct) =>
        db.Ngonngu.AsNoTracking().OrderBy(x => x.MaNn).Select(x => new LanguageInfo(x.MaNn, x.TenNn)).ToListAsync(ct);

    public Task<bool> LanguageExistsAsync(string code, CancellationToken ct) => db.Ngonngu.AnyAsync(x => x.MaNn == code, ct);

    public async Task<List<NarrationInfo>> ListAsync(CancellationToken ct) =>
        (await Full.OrderBy(x => x.MaDd).ThenBy(x => x.TieuDe).ThenByDescending(x => x.PhienBan).ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<NarrationInfo?> GetAsync(int id, CancellationToken ct) =>
        await Full.Where(x => x.MaTm == id).FirstOrDefaultAsync(ct) is { } row ? ToDto(row) : null;

    public Task<NarrationHead?> HeadAsync(int id, CancellationToken ct) =>
        db.Thuyetminh.AsNoTracking().Where(x => x.MaTm == id)
            .Select(x => new NarrationHead(x.MaTm, x.MaDd, x.TieuDe, x.PhienBan, x.TrangThai, x.Bandich.Count()))
            .FirstOrDefaultAsync(ct);

    public async Task<int> LatestVersionAsync(int destinationId, string title, CancellationToken ct) =>
        await db.Thuyetminh.Where(x => x.MaDd == destinationId && x.TieuDe == title).MaxAsync(x => (int?)x.PhienBan, ct) ?? 0;

    public Task<bool> TitleExistsAsync(int destinationId, string title, int excludeId, CancellationToken ct) =>
        db.Thuyetminh.AnyAsync(x => x.MaDd == destinationId && x.TieuDe == title && x.MaTm != excludeId, ct);

    public async Task<int> CreateAsync(int destinationId, string title, string source, int version, CancellationToken ct)
    {
        var row = new Thuyetminh { MaDd = destinationId, TieuDe = title, NoiDungGoc = source, PhienBan = version, TrangThai = ContentStatus.Draft };
        db.Thuyetminh.Add(row);
        await db.SaveChangesAsync(ct);
        return row.MaTm;
    }

    /// <summary>Sửa nội dung gốc: thuyết minh và mọi bản dịch quay về Draft để duyệt lại.</summary>
    public async Task<bool> UpdateAsync(int id, string title, string source, CancellationToken ct)
    {
        var row = await db.Thuyetminh.Include(x => x.Bandich).SingleOrDefaultAsync(x => x.MaTm == id, ct);
        if (row is null) return false;
        row.TieuDe = title;
        row.NoiDungGoc = source;
        row.TrangThai = ContentStatus.Draft;
        foreach (var translation in row.Bandich) translation.TrangThai = ContentStatus.Draft;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Đổi trạng thái; khi công bố có thể ngừng công bố các phiên bản khác cùng địa điểm và tiêu đề.</summary>
    public async Task<bool> SetStatusAsync(int id, string status, bool deactivateOtherVersions, CancellationToken ct)
    {
        var row = await db.Thuyetminh.SingleOrDefaultAsync(x => x.MaTm == id, ct);
        if (row is null) return false;
        row.TrangThai = status;
        if (deactivateOtherVersions) await DeactivateOtherVersionsAsync(row, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task DeactivateOtherVersionsAsync(Thuyetminh published, CancellationToken ct)
    {
        var others = await db.Thuyetminh.Where(x => x.MaDd == published.MaDd && x.TieuDe == published.TieuDe && x.MaTm != published.MaTm && x.TrangThai == ContentStatus.Published).ToListAsync(ct);
        foreach (var other in others) other.TrangThai = ContentStatus.Inactive;
    }

    // ---- Bản dịch ----

    public Task<TranslationHead?> TranslationAsync(int id, CancellationToken ct) =>
        db.Bandich.AsNoTracking().Where(x => x.MaBd == id).Select(x => new TranslationHead(x.MaBd, x.MaTm, x.TrangThai)).FirstOrDefaultAsync(ct);

    public Task<TranslationHead?> TranslationOfAsync(int narrationId, string languageCode, CancellationToken ct) =>
        db.Bandich.AsNoTracking().Where(x => x.MaTm == narrationId && x.MaNn == languageCode)
            .Select(x => new TranslationHead(x.MaBd, x.MaTm, x.TrangThai)).FirstOrDefaultAsync(ct);

    /// <summary>Thêm bản dịch Draft; false nếu ngôn ngữ này vừa được thêm bởi yêu cầu khác.</summary>
    public async Task<bool> AddTranslationAsync(int narrationId, string languageCode, string content, CancellationToken ct)
    {
        db.Bandich.Add(new Bandich { MaTm = narrationId, MaNn = languageCode, NoiDungDich = content, TrangThai = ContentStatus.Draft });
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException exception) when (DbErrors.IsDuplicateKey(exception))
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }

    /// <summary>Sửa nội dung bản dịch: về Draft và ngừng audio cũ vì không còn khớp nội dung.</summary>
    public async Task<bool> UpdateTranslationAsync(int id, string content, CancellationToken ct)
    {
        var row = await db.Bandich.Include(x => x.Tepaudio).SingleOrDefaultAsync(x => x.MaBd == id, ct);
        if (row is null) return false;
        row.NoiDungDich = content;
        row.TrangThai = ContentStatus.Draft;
        foreach (var audio in row.Tepaudio.Where(x => x.TrangThai != ContentStatus.Inactive)) audio.TrangThai = ContentStatus.Inactive;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetTranslationStatusAsync(int id, string status, CancellationToken ct)
    {
        var row = await db.Bandich.Include(x => x.Tepaudio).SingleOrDefaultAsync(x => x.MaBd == id, ct);
        if (row is null) return false;
        row.TrangThai = status;
        if (status == ContentStatus.Inactive)
            foreach (var audio in row.Tepaudio.Where(x => x.TrangThai == ContentStatus.Published)) audio.TrangThai = ContentStatus.Inactive;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ---- Audio ----

    public Task<AudioHead?> AudioAsync(int id, CancellationToken ct) =>
        db.Tepaudio.AsNoTracking().Where(x => x.MaTep == id)
            .Select(x => new AudioHead(x.MaTep, x.MaBd, x.MaBdNavigation.TrangThai, x.MaBdNavigation.MaTm, x.MaBdNavigation.MaTmNavigation.TrangThai, x.TrangThai, x.DuongDan))
            .FirstOrDefaultAsync(ct);

    public Task<string?> AudioFileNameAsync(int id, CancellationToken ct) =>
        db.Tepaudio.AsNoTracking().Where(x => x.MaTep == id).Select(x => (string?)x.DuongDan).FirstOrDefaultAsync(ct);

    public async Task<int> AddAudioAsync(int translationId, string voice, string fileName, string status, CancellationToken ct)
    {
        var row = new Tepaudio { MaBd = translationId, GiongDoc = voice, DuongDan = fileName, TrangThai = status };
        db.Tepaudio.Add(row);
        await db.SaveChangesAsync(ct);
        return row.MaTep;
    }

    /// <summary>Công bố audio: audio khác của bản dịch bị ngừng; bản dịch và thuyết minh cùng được công bố.</summary>
    public async Task<bool> PublishAudioAsync(int id, CancellationToken ct)
    {
        var audio = await db.Tepaudio.Include(x => x.MaBdNavigation).ThenInclude(x => x.MaTmNavigation).SingleOrDefaultAsync(x => x.MaTep == id, ct);
        if (audio is null) return false;
        var translation = audio.MaBdNavigation;
        var narration = translation.MaTmNavigation;
        foreach (var other in await db.Tepaudio.Where(x => x.MaBd == translation.MaBd && x.MaTep != id && x.TrangThai == ContentStatus.Published).ToListAsync(ct))
            other.TrangThai = ContentStatus.Inactive;
        audio.TrangThai = ContentStatus.Published;
        audio.NgayCongBo = DateTime.UtcNow;
        translation.TrangThai = ContentStatus.Published;
        if (narration.TrangThai != ContentStatus.Published)
        {
            narration.TrangThai = ContentStatus.Published;
            await DeactivateOtherVersionsAsync(narration, ct);
        }
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Ngừng công bố audio; nếu bản dịch không còn audio nào đang công bố thì bản dịch về Approved.</summary>
    public async Task<bool> UnpublishAudioAsync(int id, CancellationToken ct)
    {
        var audio = await db.Tepaudio.Include(x => x.MaBdNavigation).SingleOrDefaultAsync(x => x.MaTep == id, ct);
        if (audio is null) return false;
        var translation = audio.MaBdNavigation;
        audio.TrangThai = ContentStatus.Inactive;
        var stillPublished = await db.Tepaudio.AnyAsync(x => x.MaBd == translation.MaBd && x.MaTep != id && x.TrangThai == ContentStatus.Published, ct);
        if (!stillPublished && translation.TrangThai == ContentStatus.Published) translation.TrangThai = ContentStatus.Approved;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
