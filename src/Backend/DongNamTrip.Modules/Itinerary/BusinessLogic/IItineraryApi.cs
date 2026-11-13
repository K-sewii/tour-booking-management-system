using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Itinerary;

public interface IItineraryApi
{
    Task<Contracts.Itinerary?> GetAsync(int id, int accountId, CancellationToken ct);

    /// <summary>Tạo phiên bản bất biến của lịch trình (chỉ chủ lịch trình). Khóa dòng bằng UPDLOCK trong giao dịch của người gọi; không tự mở giao dịch.</summary>
    Task<OpResult<ItineraryVersionRef>> CreateVersionAsync(int itineraryId, int accountId, CancellationToken ct);
    Task<ItineraryVersion?> VersionAsync(int versionId, CancellationToken ct);
    Task<ItineraryCheck> CheckVersionAsync(int versionId, CancellationToken ct);
    /// <summary>Các điểm của một phiên bản theo ngày và thứ tự; rỗng nếu không có phiên bản.</summary>
    Task<List<ProgramStop>> ProgramAsync(int versionId, CancellationToken ct);
}
