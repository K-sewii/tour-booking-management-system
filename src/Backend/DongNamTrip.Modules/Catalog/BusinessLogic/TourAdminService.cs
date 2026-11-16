using DongNamTrip.Contracts;
using DongNamTrip.Modules.Administration;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Quản trị tour và lịch khởi hành (UC16, SCR16): STM-05, STM-06, QT01, QT09.</summary>
public sealed class TourAdminService(TourAdminRepository repository, IAdministrationApi administration, TimeProvider time)
{
    private const string TourType = "TOUR";
    private const string DepartureType = "LICHKHOIHANH";

    private DateTime Now => time.GetLocalNow().DateTime;

    public Task<List<AdminTour>> ListAsync(string? status, CancellationToken ct) => repository.ListAsync(status, ct);

    public Task<AdminTourDetail?> GetAsync(int id, CancellationToken ct) => repository.GetAsync(id, ct);

    public async Task<OpResult<AdminTour>> SaveAsync(int? id, TourForm form, int actor, CancellationToken ct)
    {
        var clean = form with
        {
            Name = (form.Name ?? "").Trim(), Duration = (form.Duration ?? "").Trim(), Summary = (form.Summary ?? "").Trim(),
            CancelPolicy = string.IsNullOrWhiteSpace(form.CancelPolicy) ? null : form.CancelPolicy.Trim()
        };
        var errors = new List<string>();
        if (clean.Name.Length is < 1 or > 200) errors.Add("Tên tour 1–200 ký tự.");
        if (!CatalogOptions.Regions.Contains(clean.Region)) errors.Add("Khu vực không hợp lệ.");
        if (!CatalogOptions.Categories.Contains(clean.Category)) errors.Add("Loại hình không hợp lệ.");
        if (clean.Duration.Length is < 1 or > 50) errors.Add("Thời lượng 1–50 ký tự.");
        if (clean.ReferencePrice < 0) errors.Add("Giá tham khảo không được âm.");
        if (clean.Summary.Length > 1000) errors.Add("Tóm tắt tối đa 1000 ký tự.");
        if (clean.CancelPolicy?.Length > 2000) errors.Add("Điều kiện hủy tối đa 2000 ký tự.");
        if (errors.Count > 0) return Op.Invalid(string.Join(" ", errors));

        TourForm? before = null;
        if (id is not null && (before = await repository.FormAsync(id.Value, ct)) is null)
            return Op.NotFound("Không tìm thấy tour.");
        if (await repository.NameExistsAsync(clean.Name, clean.Region, id, ct))
            return Op.Conflict("Tour này đã có trong khu vực đã chọn.");
        var saved = await repository.SaveAsync(id, clean, ct);
        if (saved is null) return Op.NotFound("Không tìm thấy tour.");
        await administration.RecordAsync(new AuditEntry(actor, id is null ? AuditActions.TourCreated : AuditActions.TourEdited, TourType, saved.Id, null, before, clean), ct);
        return Op.Ok(saved);
    }

    public async Task<OpResult<AdminTourDetail>> SetPlacesAsync(int id, IReadOnlyList<TourStopForm> stops, int actor, CancellationToken ct)
    {
        var status = await repository.StatusAsync(id, ct);
        if (status is null) return Op.NotFound("Không tìm thấy tour.");
        if (stops.Count > Limits.MaxPlaces) return Op.Invalid($"Tối đa {Limits.MaxPlaces} điểm dừng.");
        if (stops.Any(stop => stop.Day < 1 || stop.Order < 1)) return Op.Invalid("Ngày thứ và thứ tự phải từ 1.");
        if (stops.Select(stop => stop.DestinationId).Distinct().Count() != stops.Count) return Op.Invalid("Một địa điểm chỉ xuất hiện một lần trong tour.");
        if (stops.Select(stop => (stop.Day, stop.Order)).Distinct().Count() != stops.Count) return Op.Invalid("Cặp (ngày thứ, thứ tự) bị trùng.");
        if (status == TourStatus.Published && stops.Count == 0) return Op.Invalid("Tour đã công bố cần ít nhất một điểm dừng.");
        var published = await repository.PublishedPlaceIdsAsync(stops.Select(stop => stop.DestinationId).ToArray(), ct);
        if (published.Count != stops.Count) return Op.Invalid("Địa điểm không tồn tại hoặc chưa công bố.");
        if (await repository.HasHeldSeatsAsync(id, ct))
            return Op.Conflict("Tour đã có chỗ được giữ; muốn đổi chương trình hãy điều chỉnh đơn đặt tour.");

        var before = await repository.GetAsync(id, ct);
        await using (var transaction = await repository.BeginAsync(ct))
        {
            await repository.ReplacePlacesAsync(id, stops, ct);
            await transaction.CommitAsync(ct);
        }
        var after = (await repository.GetAsync(id, ct))!;
        await administration.RecordAsync(new AuditEntry(actor, AuditActions.TourStopsEdited, TourType, id, null, before!.Stops, after.Stops), ct);
        return Op.Ok(after);
    }

    public async Task<OpResult<AdminTour>> SetStatusAsync(int id, string status, string? reason, int actor, CancellationToken ct)
    {
        if (status is not (TourStatus.Published or TourStatus.Inactive)) return Op.Invalid("Trạng thái chỉ nhận Published hoặc Inactive.");
        var before = await repository.TourAsync(id, ct);
        if (before is null) return Op.NotFound("Không tìm thấy tour.");
        var allowed = (before.Status, status) is (TourStatus.Draft, TourStatus.Published) or (TourStatus.Inactive, TourStatus.Published)
            or (TourStatus.Published, TourStatus.Inactive);
        if (!allowed) return Op.Conflict($"Không thể chuyển tour từ \"{ContentStatus.Label(before.Status)}\" sang \"{ContentStatus.Label(status)}\".");
        if (status == TourStatus.Published)
        {
            var form = (await repository.FormAsync(id, ct))!;
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(form.Name)) missing.Add("tên");
            if (string.IsNullOrWhiteSpace(form.Region)) missing.Add("khu vực");
            if (string.IsNullOrWhiteSpace(form.Category)) missing.Add("loại hình");
            if (string.IsNullOrWhiteSpace(form.Duration)) missing.Add("thời lượng");
            if (form.ReferencePrice <= 0) missing.Add("giá tham khảo");
            if (string.IsNullOrWhiteSpace(form.Summary)) missing.Add("tóm tắt");
            if (await repository.StopCountAsync(id, ct) == 0) missing.Add("điểm dừng");
            if (missing.Count > 0) return Op.Unprocessable("Tour chưa đủ thông tin để công bố: " + string.Join(", ", missing) + ".");
        }
        if (!await repository.SetStatusAsync(id, status, ct)) return Op.NotFound("Không tìm thấy tour.");
        var after = (await repository.TourAsync(id, ct))!;
        await administration.RecordAsync(new AuditEntry(actor, status == TourStatus.Published ? AuditActions.TourPublished : AuditActions.TourStopped, TourType, id, reason, before, after), ct);
        return Op.Ok(after);
    }

    public async Task<OpResult<List<AdminDeparture>>> DeparturesAsync(int tourId, CancellationToken ct) =>
        await repository.TourAsync(tourId, ct) is null ? Op.NotFound("Không tìm thấy tour.") : Op.Ok(await repository.DeparturesAsync(tourId, ct));

    public async Task<OpResult<AdminDeparture>> SaveDepartureAsync(int tourId, int? id, DepartureForm form, int actor, CancellationToken ct)
    {
        AdminDeparture? before = null;
        if (id is not null)
        {
            before = await repository.DepartureAsync(id.Value, ct);
            if (before is null) return Op.NotFound("Không tìm thấy lịch khởi hành.");
            tourId = before.TourId;
        }
        else
        {
            var status = await repository.StatusAsync(tourId, ct);
            if (status is null) return Op.NotFound("Không tìm thấy tour.");
            // 400 cho dữ liệu sai được kiểm tra trước; tour chưa công bố là 422.
        }

        var errors = new List<string>();
        var dateChanged = before is null || form.StartsAt != before.StartsAt;
        if (dateChanged && form.StartsAt <= Now) errors.Add("Ngày giờ khởi hành phải sau thời điểm hiện tại.");
        if (form.Price <= 0) errors.Add("Giá phải lớn hơn 0.");
        if (form.Capacity <= 0) errors.Add("Sức chứa phải lớn hơn 0.");
        if (errors.Count > 0) return Op.Invalid(string.Join(" ", errors));

        if (before is null)
        {
            if (await repository.StatusAsync(tourId, ct) != TourStatus.Published) return Op.Unprocessable("Chỉ tạo lịch khởi hành cho tour đã công bố.");
        }
        else
        {
            if (form.Capacity < before.Held) return Op.Conflict($"Sức chứa không được nhỏ hơn số chỗ đã giữ ({before.Held}).");
            if (before.Held > 0 && form.StartsAt != before.StartsAt) return Op.Conflict("Lịch đã có chỗ được giữ nên không đổi được ngày giờ khởi hành.");
        }
        var saved = await repository.SaveDepartureAsync(tourId, id, form, ct);
        if (saved is null) return Op.NotFound("Không tìm thấy lịch khởi hành.");
        await administration.RecordAsync(new AuditEntry(actor, id is null ? AuditActions.DepartureCreated : AuditActions.DepartureEdited, DepartureType, saved.Id, null, before, saved), ct);
        return Op.Ok(saved);
    }

    public async Task<OpResult<AdminDeparture>> DeactivateDepartureAsync(int id, int actor, CancellationToken ct)
    {
        var before = await repository.DepartureAsync(id, ct);
        if (before is null) return Op.NotFound("Không tìm thấy lịch khởi hành.");
        if (before.Status == DepartureStatus.Inactive) return Op.Conflict("Lịch khởi hành đã ngừng.");
        await repository.SetDepartureStatusAsync(id, DepartureStatus.Inactive, ct);
        var after = (await repository.DepartureAsync(id, ct))!;
        await administration.RecordAsync(new AuditEntry(actor, AuditActions.DepartureStopped, DepartureType, id, null, before, after), ct);
        return Op.Ok(after);
    }
}
