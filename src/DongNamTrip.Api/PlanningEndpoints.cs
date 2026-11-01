using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using DongNamTrip.Api.Data;
using DongNamTrip.Api.Data.Entities;
using DongNamTrip.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Api;

public static class PlanningEndpoints
{
    public static void MapPlanningEndpoints(this WebApplication app)
    {
        app.MapGet("/api/planner/catalog", async (PlanningService service, CancellationToken ct) =>
            Results.Ok(await service.CatalogAsync(ct)));
        app.MapGet("/api/planner/tours/{id:int}", async (int id, PlanningService service, CancellationToken ct) =>
            await service.TourTemplateAsync(id, ct) is { } stops ? Results.Ok(stops) : Results.NotFound());

        var itineraries = app.MapGroup("/api/itineraries").RequireAuthorization();
        itineraries.MapGet("", async (ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
            Results.Ok(await service.ItinerariesAsync(AccountId(user), ct)));
        itineraries.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
            await service.ItineraryAsync(id, AccountId(user), ct) is { } item ? Results.Ok(item) : Results.NotFound());
        itineraries.MapPost("", (ItineraryDraft draft, ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
            service.SaveAsync(null, AccountId(user), draft, ct));
        itineraries.MapPut("/{id:int}", (int id, ItineraryDraft draft, ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
            service.SaveAsync(id, AccountId(user), draft, ct));
        itineraries.MapPost("/{id:int}/requests", (int id, PrivateTourRequestDraft draft, ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
            service.SubmitAsync(id, AccountId(user), draft, ct));

        var requests = app.MapGroup("/api/private-tour-requests").RequireAuthorization();
        requests.MapGet("", async (ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
            Results.Ok(await service.RequestsAsync(AccountId(user), false, ct)));
        requests.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
            await service.RequestAsync(id, AccountId(user), IsStaff(user), ct) is { } item ? Results.Ok(item) : Results.NotFound());
        requests.MapPost("/{id:int}/quotes/{quoteId:int}/accept", (int id, int quoteId, ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
            service.AcceptAsync(id, quoteId, AccountId(user), ct));
        app.MapGet("/api/private-tour-orders", async (ClaimsPrincipal user, PlanningService service, CancellationToken ct) =>
            Results.Ok(await service.OrdersAsync(AccountId(user), ct))).RequireAuthorization();

        var staff = app.MapGroup("/api/staff/private-tour-requests")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Operator));
        staff.MapGet("", async (PlanningService service, CancellationToken ct) =>
            Results.Ok(await service.RequestsAsync(0, true, ct)));
        staff.MapPost("/{id:int}/verifications", (int id, ServiceVerificationDraft draft, PlanningService service, CancellationToken ct) =>
            service.VerifyAsync(id, draft, ct));
        staff.MapPost("/{id:int}/quotes", (int id, QuoteDraft draft, PlanningService service, CancellationToken ct) =>
            service.IssueQuoteAsync(id, draft, ct));
    }

    private static int AccountId(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static bool IsStaff(ClaimsPrincipal user) => user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.Operator);
}

public sealed class PlanningService(DongNamTripDbContext db)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<PlannerCatalog> CatalogAsync(CancellationToken ct)
    {
        var places = await db.Diadiem.AsNoTracking().Where(x => x.TrangThai == "Published")
            .OrderBy(x => x.MaDd).ToListAsync(ct);
        var activities = await db.Hoatdong.AsNoTracking().Where(x => x.TrangThai == "Published" && x.MaDdNavigation.TrangThai == "Published")
            .OrderBy(x => x.MaHd).Select(x => new PlannerActivity(x.MaHd, x.MaDd, x.TenHd, x.ThoiLuongPhut, x.DieuKien, x.PhiThamKhao)).ToListAsync(ct);
        var slots = await db.Suat.AsNoTracking().Where(x => x.MaHdNavigation.TrangThai == "Published" && x.MaHdNavigation.MaDdNavigation.TrangThai == "Published" && x.Ngay >= DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(x => x.Ngay).ThenBy(x => x.GioBatDau)
            .Select(x => new PlannerSlot(x.MaSuat, x.MaHd, x.MaHdNavigation.MaDd, x.Ngay, x.GioBatDau, x.GioKetThuc, x.TrangThai)).ToListAsync(ct);
        return new PlannerCatalog(places.Select(x => new Destination(x.MaDd, x.TenDd, x.KhuVuc, x.LoaiHinh, x.DiaChi, x.MoTa, x.ThoiLuongPhut, CatalogOptions.IllustrationFor(x.KhuVuc))).ToList(), activities, slots);
    }

    public async Task<ItineraryStopDraft[]?> TourTemplateAsync(int tourId, CancellationToken ct)
    {
        if (!await db.Tour.AsNoTracking().AnyAsync(x => x.MaTour == tourId && x.TrangThai == "Published", ct))
            return null;
        var stops = await db.TourDiadiem.AsNoTracking().Where(x => x.MaTour == tourId)
            .OrderBy(x => x.NgayThu).ThenBy(x => x.ThuTu).ToListAsync(ct);
        return stops.Select(x => new ItineraryStopDraft { DestinationId = x.MaDd, Day = x.NgayThu, Order = x.ThuTu }).ToArray();
    }

    public async Task<List<Itinerary>> ItinerariesAsync(int accountId, CancellationToken ct)
    {
        var rows = await db.Lichtrinh.AsNoTracking().Where(x => x.MaTk == accountId)
            .OrderByDescending(x => x.NgayCapNhat).ToListAsync(ct);
        var result = new List<Itinerary>(rows.Count);
        foreach (var row in rows) result.Add(await ToItineraryAsync(row, ct));
        return result;
    }

    public async Task<Itinerary?> ItineraryAsync(int id, int accountId, CancellationToken ct)
    {
        var row = await db.Lichtrinh.AsNoTracking().SingleOrDefaultAsync(x => x.MaLt == id && x.MaTk == accountId, ct);
        return row is null ? null : await ToItineraryAsync(row, ct);
    }

    public async Task<IResult> SaveAsync(int? id, int accountId, ItineraryDraft draft, CancellationToken ct)
    {
        var stops = NormalizeStops(draft);
        if (!Valid(draft) || draft.StartDate < DateOnly.FromDateTime(DateTime.Today) || stops is null)
            return Error("Lịch trình cần tên, ngày hợp lệ, 1–12 điểm khác nhau và thứ tự hợp lệ.");
        var placeIds = stops.Select(x => x.DestinationId).ToArray();
        if (await db.Diadiem.CountAsync(x => placeIds.Contains(x.MaDd) && x.TrangThai == "Published", ct) != placeIds.Length)
            return Error("Địa điểm không tồn tại hoặc chưa công bố.");
        var slotIds = stops.SelectMany(x => x.SlotIds).ToArray();
        if (slotIds.Length != slotIds.Distinct().Count()) return Error("Một suất chỉ được chọn một lần.");
        var slots = await db.Suat.AsNoTracking().Include(x => x.MaHdNavigation)
            .Where(x => slotIds.Contains(x.MaSuat)).ToListAsync(ct);
        if (slots.Count != slotIds.Length || slots.Any(slot => !stops.Any(stop => stop.DestinationId == slot.MaHdNavigation.MaDd && stop.SlotIds.Contains(slot.MaSuat))))
            return Error("Suất phải thuộc hoạt động tại điểm đã chọn.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var row = id is null ? new Lichtrinh { MaTk = accountId } :
            await db.Lichtrinh.SingleOrDefaultAsync(x => x.MaLt == id && x.MaTk == accountId, ct);
        if (row is null) return Results.NotFound();
        row.TenLt = draft.Name.Trim();
        row.NgayDi = draft.StartDate;
        row.SoKhach = draft.Guests;
        row.NoiXuatPhat = draft.DeparturePoint?.Trim();
        row.PhuongTien = draft.Transport?.Trim();
        row.NganSach = draft.Budget;
        row.NgayCapNhat = DateTime.UtcNow;
        if (id is null) { db.Lichtrinh.Add(row); await db.SaveChangesAsync(ct); }
        else
        {
            await db.LichtrinhSuat.Where(x => x.MaLt == row.MaLt).ExecuteDeleteAsync(ct);
            await db.LichtrinhDiadiem.Where(x => x.MaLt == row.MaLt).ExecuteDeleteAsync(ct);
        }
        foreach (var stop in stops)
        {
            db.LichtrinhDiadiem.Add(new LichtrinhDiadiem { MaLt = row.MaLt, MaDd = stop.DestinationId, NgayThu = stop.Day, ThuTu = stop.Order, GhiChu = stop.Note?.Trim() });
        }
        await db.SaveChangesAsync(ct);
        foreach (var stop in stops)
            foreach (var slotId in stop.SlotIds)
                db.LichtrinhSuat.Add(new LichtrinhSuat { MaLt = row.MaLt, MaDd = stop.DestinationId, MaSuat = slotId });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(await ToItineraryAsync(row, ct));
    }

    public async Task<IResult> SubmitAsync(int itineraryId, int accountId, PrivateTourRequestDraft draft, CancellationToken ct)
    {
        if (!Valid(draft) || string.IsNullOrWhiteSpace(draft.Contact)) return Error("Cần thông tin liên hệ để gửi yêu cầu.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var itinerary = await ItineraryAsync(itineraryId, accountId, ct);
        if (itinerary is null) return Results.NotFound();
        if (itinerary.StartDate < DateOnly.FromDateTime(DateTime.Today) || itinerary.Guests < 1 || itinerary.Stops.Length == 0)
            return Error("Cần ngày đi, số khách và địa điểm hợp lệ trước khi gửi.");
        var snapshot = JsonSerializer.Serialize(itinerary, Json);
        var latest = await db.Phienbanlichtrinh.Where(x => x.MaLt == itineraryId)
            .OrderByDescending(x => x.SoPhienBan).FirstOrDefaultAsync(ct);
        if (latest is not null && latest.NoiDungJson == snapshot)
        {
            var existing = await db.Yeucautourrieng.AsNoTracking().Where(x => x.MaPb == latest.MaPb && x.LienHe == draft.Contact.Trim() && x.NhuCauDichVu == draft.ServiceNeeds)
                .OrderByDescending(x => x.MaYc).FirstOrDefaultAsync(ct);
            if (existing is not null)
                return Results.Ok(await RequestAsync(existing.MaYc, accountId, false, ct));
        }
        var version = new Phienbanlichtrinh { MaLt = itineraryId, SoPhienBan = (latest?.SoPhienBan ?? 0) + 1, ThoiDiemTao = DateTime.UtcNow, NoiDungJson = snapshot };
        db.Phienbanlichtrinh.Add(version);
        await db.SaveChangesAsync(ct);
        var request = new Yeucautourrieng { MaPb = version.MaPb, SoKhach = itinerary.Guests, LienHe = draft.Contact.Trim(), NhuCauDichVu = draft.ServiceNeeds?.Trim(), NgayGui = DateTime.UtcNow, TrangThai = "Submitted" };
        db.Yeucautourrieng.Add(request);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(await RequestAsync(request.MaYc, accountId, false, ct));
    }

    public async Task<List<PrivateTourRequest>> RequestsAsync(int accountId, bool staff, CancellationToken ct)
    {
        var ids = await db.Yeucautourrieng.AsNoTracking()
            .Where(x => staff || x.MaPbNavigation.MaLtNavigation.MaTk == accountId)
            .OrderByDescending(x => x.NgayGui).Select(x => x.MaYc).ToListAsync(ct);
        var items = new List<PrivateTourRequest>(ids.Count);
        foreach (var id in ids)
            if (await RequestAsync(id, accountId, staff, ct) is { } item) items.Add(item);
        return items;
    }

    public async Task<PrivateTourRequest?> RequestAsync(int id, int accountId, bool staff, CancellationToken ct)
    {
        var row = await db.Yeucautourrieng.AsNoTracking()
            .Include(x => x.MaPbNavigation).ThenInclude(x => x.MaLtNavigation).ThenInclude(x => x.MaTkNavigation)
            .Include(x => x.Xacminhdichvu)
            .Include(x => x.Baogia).ThenInclude(x => x.Hangmucbaogia)
            .Include(x => x.Baogia).ThenInclude(x => x.Dondat)
            .AsSplitQuery().SingleOrDefaultAsync(x => x.MaYc == id && (staff || x.MaPbNavigation.MaLtNavigation.MaTk == accountId), ct);
        if (row is null) return null;
        var version = row.MaPbNavigation;
        var snapshot = JsonSerializer.Deserialize<Itinerary>(version.NoiDungJson, Json)!;
        return new PrivateTourRequest(row.MaYc, version.MaLt, version.SoPhienBan, version.MaLtNavigation.MaTk,
            version.MaLtNavigation.MaTkNavigation.HoTen, row.LienHe, row.NhuCauDichVu, row.NgayGui, row.TrangThai,
            snapshot,
            row.Xacminhdichvu.OrderBy(x => x.MaXm).Select(x => new ServiceVerification(x.MaXm, x.HangMuc, x.DonViCungCap, x.KetQua, x.GhiChu, x.ThoiDiem)).ToList(),
            row.Baogia.OrderBy(x => x.MaBg).Select(x => new PrivateTourQuote(x.MaBg, row.MaYc, version.SoPhienBan, x.TongGia, x.KhoanCanThu, x.DieuKienHuy, x.HanHieuLuc, x.TrangThai == "Issued" && x.HanHieuLuc <= DateTime.UtcNow ? "Expired" : x.TrangThai,
                x.Hangmucbaogia.OrderBy(line => line.MaHm).Select(line => new QuoteLine(line.MaHm, line.MoTa, line.DonViTinh, line.SoLuong, line.DonGia)).ToList(), x.Dondat?.MaDon)).ToList());
    }

    public async Task<IResult> VerifyAsync(int id, ServiceVerificationDraft draft, CancellationToken ct)
    {
        if (!Valid(draft) || string.IsNullOrWhiteSpace(draft.Item) || string.IsNullOrWhiteSpace(draft.Supplier) || draft.Result is not ("Available" or "Unavailable" or "NeedsInfo"))
            return Error("Cần hạng mục, đơn vị cung cấp và kết quả xác minh hợp lệ.");
        var request = await db.Yeucautourrieng.SingleOrDefaultAsync(x => x.MaYc == id, ct);
        if (request is null) return Results.NotFound();
        if (request.TrangThai == "Accepted") return Results.Conflict(new { message = "Yêu cầu đã được chốt." });
        var item = new Xacminhdichvu { MaYc = id, HangMuc = draft.Item.Trim(), DonViCungCap = draft.Supplier.Trim(), KetQua = draft.Result, GhiChu = draft.Note?.Trim(), ThoiDiem = DateTime.UtcNow };
        db.Xacminhdichvu.Add(item);
        request.TrangThai = draft.Result == "NeedsInfo" ? "NeedsInfo" : "Reviewing";
        await db.SaveChangesAsync(ct);
        return Results.Ok(new ServiceVerification(item.MaXm, item.HangMuc, item.DonViCungCap, item.KetQua, item.GhiChu, item.ThoiDiem));
    }

    public async Task<IResult> IssueQuoteAsync(int id, QuoteDraft draft, CancellationToken ct)
    {
        if (!Valid(draft) || draft.Lines.Length == 0 || draft.ExpiresAt <= DateTime.UtcNow || string.IsNullOrWhiteSpace(draft.CancellationTerms)
            || draft.Lines.Any(x => !Valid(x) || string.IsNullOrWhiteSpace(x.Description) || string.IsNullOrWhiteSpace(x.Unit)))
            return Error("Báo giá cần hạng mục, điều kiện hủy và hạn hiệu lực tương lai.");
        decimal total;
        try { total = draft.Lines.Sum(x => checked(x.Quantity * x.UnitPrice)); }
        catch (OverflowException) { return Error("Tổng giá vượt giới hạn."); }
        if (total > 999999999999999999 || draft.AmountDue > total) return Error("Khoản cần thu phải nằm trong tổng giá.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await db.Yeucautourrieng.SingleOrDefaultAsync(x => x.MaYc == id, ct);
        if (request is null) return Results.NotFound();
        if (request.TrangThai == "Accepted") return Results.Conflict(new { message = "Yêu cầu đã được chốt." });
        if (!await db.Xacminhdichvu.AnyAsync(x => x.MaYc == id && x.KetQua == "Available", ct))
            return Results.Conflict(new { message = "Cần xác minh ít nhất một dịch vụ có thể tổ chức trước khi phát hành báo giá." });
        var old = await db.Baogia.Where(x => x.MaYc == id && x.TrangThai == "Issued").ToListAsync(ct);
        foreach (var item in old) item.TrangThai = "Replaced";
        var quote = new Baogia { MaYc = id, MaPb = request.MaPb, TongGia = total, KhoanCanThu = draft.AmountDue, DieuKienHuy = draft.CancellationTerms.Trim(), HanHieuLuc = draft.ExpiresAt.ToUniversalTime(), TrangThai = "Issued" };
        db.Baogia.Add(quote);
        await db.SaveChangesAsync(ct);
        foreach (var line in draft.Lines)
            db.Hangmucbaogia.Add(new Hangmucbaogia { MaBg = quote.MaBg, MoTa = line.Description.Trim(), DonViTinh = line.Unit.Trim(), SoLuong = line.Quantity, DonGia = line.UnitPrice });
        request.TrangThai = "Quoted";
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(await RequestAsync(id, 0, true, ct));
    }

    public async Task<IResult> AcceptAsync(int requestId, int quoteId, int accountId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var quote = await db.Baogia.Include(x => x.Dondat).Include(x => x.MaYcNavigation).ThenInclude(x => x.MaPbNavigation).ThenInclude(x => x.MaLtNavigation)
            .SingleOrDefaultAsync(x => x.MaBg == quoteId && x.MaYc == requestId, ct);
        if (quote is null || quote.MaYcNavigation.MaPbNavigation.MaLtNavigation.MaTk != accountId) return Results.NotFound();
        if (quote.Dondat is { } existing)
            return Results.Ok(ToOrder(existing));
        if (quote.TrangThai != "Issued" || quote.HanHieuLuc <= DateTime.UtcNow)
            return Results.Conflict(new { message = "Báo giá đã hết hạn hoặc đã được thay thế." });
        var current = await ItineraryAsync(quote.MaYcNavigation.MaPbNavigation.MaLt, accountId, ct);
        var snapshot = JsonSerializer.Deserialize<Itinerary>(quote.MaYcNavigation.MaPbNavigation.NoiDungJson, Json)!;
        if (current is null || JsonSerializer.Serialize(current with { Warnings = [] }, Json) != JsonSerializer.Serialize(snapshot with { Warnings = [] }, Json))
            return Results.Conflict(new { message = "Lịch trình đã đổi sau khi gửi yêu cầu; vui lòng xin báo giá mới." });
        var order = new Dondat { MaTk = accountId, MaBg = quoteId, SoKhach = quote.MaYcNavigation.SoKhach, GiaChot = quote.TongGia, DieuKienHuy = quote.DieuKienHuy, TrangThai = "Pending", NgayDat = DateTime.UtcNow };
        db.Dondat.Add(order);
        quote.TrangThai = "Accepted";
        quote.MaYcNavigation.TrangThai = "Accepted";
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(ToOrder(order));
    }

    public async Task<List<PrivateTourOrder>> OrdersAsync(int accountId, CancellationToken ct)
    {
        var rows = await db.Dondat.AsNoTracking().Where(x => x.MaTk == accountId && x.MaBg != null)
            .OrderByDescending(x => x.NgayDat).ToListAsync(ct);
        return rows.Select(ToOrder).ToList();
    }

    private async Task<Itinerary> ToItineraryAsync(Lichtrinh row, CancellationToken ct)
    {
        var stops = await db.LichtrinhDiadiem.AsNoTracking().Where(x => x.MaLt == row.MaLt)
            .OrderBy(x => x.NgayThu).ThenBy(x => x.ThuTu).ToListAsync(ct);
        var selected = await db.LichtrinhSuat.AsNoTracking().Where(x => x.MaLt == row.MaLt).ToListAsync(ct);
        var slotIds = selected.Select(x => x.MaSuat).ToArray();
        var slots = await db.Suat.AsNoTracking().Where(x => slotIds.Contains(x.MaSuat)).ToListAsync(ct);
        var stopDtos = stops.Select(x => new ItineraryStopDraft
        {
            DestinationId = x.MaDd, Day = x.NgayThu, Order = x.ThuTu, Note = x.GhiChu,
            SlotIds = selected.Where(slot => slot.MaDd == x.MaDd).OrderBy(slot => slot.MaSuat).Select(slot => slot.MaSuat).ToArray()
        }).ToArray();
        var warnings = new List<string>();
        foreach (var stop in stopDtos)
        {
            var chosen = slots.Where(slot => stop.SlotIds.Contains(slot.MaSuat)).OrderBy(slot => slot.GioBatDau).ToList();
            foreach (var slot in chosen)
            {
                if (slot.Ngay != row.NgayDi.AddDays(stop.Day - 1)) warnings.Add($"Suất #{slot.MaSuat} không đúng ngày thứ {stop.Day}.");
                if (slot.TrangThai != "Published") warnings.Add($"Suất #{slot.MaSuat} đã ngừng công bố.");
            }
        }
        foreach (var day in slots.GroupBy(x => x.Ngay))
        {
            var daySlots = day.ToArray();
            for (var i = 0; i < daySlots.Length; i++)
                for (var j = i + 1; j < daySlots.Length; j++)
                    if (daySlots[i].GioBatDau < daySlots[j].GioKetThuc && daySlots[j].GioBatDau < daySlots[i].GioKetThuc)
                        warnings.Add($"Các suất #{daySlots[i].MaSuat} và #{daySlots[j].MaSuat} trùng giờ.");
        }
        if (stopDtos.GroupBy(x => x.Day).Any(group => group.Count() > 1))
            warnings.Add("Chưa có dữ liệu thời gian di chuyển giữa các điểm; cần điều hành xác minh.");
        return new Itinerary(row.MaLt, row.MaTk, row.TenLt, row.NgayDi, row.SoKhach, stopDtos.Select(x => x.DestinationId).ToArray())
        { DeparturePoint = row.NoiXuatPhat, Transport = row.PhuongTien, Budget = row.NganSach, Stops = stopDtos, Warnings = warnings.ToArray() };
    }

    private static ItineraryStopDraft[]? NormalizeStops(ItineraryDraft draft)
    {
        var stops = draft.Stops is { Length: > 0 } ? draft.Stops :
            (draft.DestinationIds ?? []).Select((id, index) => new ItineraryStopDraft { DestinationId = id, Day = 1, Order = index + 1 }).ToArray();
        if (stops.Length is < 1 or > 12 || stops.Any(x => x.DestinationId < 1 || x.Day is < 1 or > 30 || x.Order is < 1 or > 12 || x.Note?.Length > 500 || x.SlotIds is null || x.SlotIds.Length > 20)
            || stops.Select(x => x.DestinationId).Distinct().Count() != stops.Length
            || stops.GroupBy(x => (x.Day, x.Order)).Any(x => x.Count() > 1)) return null;
        return stops;
    }

    private static bool Valid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), [], true);
    private static IResult Error(string message) => Results.BadRequest(new { message });
    private static PrivateTourOrder ToOrder(Dondat row) => new(row.MaDon, row.MaBg!.Value, row.MaTk, row.SoKhach, row.GiaChot, row.DieuKienHuy, row.TrangThai, row.NgayDat);
}
