using System.Data;
using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Quotation;

public enum QuotationWriteStatus { Saved, NotFound, Conflict, NeedsVerification, StaleItinerary }
public sealed record QuotationWrite<T>(QuotationWriteStatus Status, T? Value = default);

/// <summary>Persists private-tour requests, itinerary snapshots, verification, quotes and accepted orders.</summary>
public sealed class QuotationRepository(DongNamTripDbContext db)
{
    public Task<List<int>> RequestIdsAsync(int accountId, bool staff, CancellationToken ct) =>
        db.Yeucautourrieng.AsNoTracking().Where(item => staff || item.MaPbNavigation.MaLtNavigation.MaTk == accountId)
            .OrderByDescending(item => item.NgayGui).Select(item => item.MaYc).ToListAsync(ct);

    public Task<Yeucautourrieng?> RequestAsync(int id, int accountId, bool staff, CancellationToken ct) =>
        db.Yeucautourrieng.AsNoTracking()
            .Include(item => item.MaPbNavigation).ThenInclude(version => version.MaLtNavigation).ThenInclude(trip => trip.MaTkNavigation)
            .Include(item => item.Xacminhdichvu)
            .Include(item => item.Baogia).ThenInclude(quote => quote.Hangmucbaogia)
            .Include(item => item.Baogia).ThenInclude(quote => quote.Dondat)
            .AsSplitQuery().SingleOrDefaultAsync(item => item.MaYc == id && (staff || item.MaPbNavigation.MaLtNavigation.MaTk == accountId), ct);

    public async Task<int> SubmitAsync(int itineraryId, string snapshotJson, int guests, PrivateTourRequestDraft draft, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var latest = await db.Phienbanlichtrinh.Where(item => item.MaLt == itineraryId)
            .OrderByDescending(item => item.SoPhienBan).FirstOrDefaultAsync(ct);
        var needs = draft.ServiceNeeds?.Trim();
        if (latest?.NoiDungJson == snapshotJson)
        {
            var existing = await db.Yeucautourrieng.AsNoTracking().Where(item => item.MaPb == latest.MaPb
                && item.LienHe == draft.Contact.Trim() && item.NhuCauDichVu == needs)
                .OrderByDescending(item => item.MaYc).FirstOrDefaultAsync(ct);
            if (existing is not null) return existing.MaYc;
        }
        var version = new Phienbanlichtrinh { MaLt = itineraryId, SoPhienBan = (latest?.SoPhienBan ?? 0) + 1,
            ThoiDiemTao = DateTime.UtcNow, NoiDungJson = snapshotJson };
        db.Phienbanlichtrinh.Add(version);
        await db.SaveChangesAsync(ct);
        var request = new Yeucautourrieng { MaPb = version.MaPb, SoKhach = guests, LienHe = draft.Contact.Trim(),
            NhuCauDichVu = needs, NgayGui = DateTime.UtcNow, TrangThai = "Submitted" };
        db.Yeucautourrieng.Add(request);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return request.MaYc;
    }

    public async Task<QuotationWrite<Xacminhdichvu>> VerifyAsync(int id, ServiceVerificationDraft draft, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await db.Yeucautourrieng.SingleOrDefaultAsync(item => item.MaYc == id, ct);
        if (request is null) return new(QuotationWriteStatus.NotFound);
        if (request.TrangThai == "Accepted") return new(QuotationWriteStatus.Conflict);
        var item = new Xacminhdichvu { MaYc = id, HangMuc = draft.Item.Trim(), DonViCungCap = draft.Supplier.Trim(),
            KetQua = draft.Result, GhiChu = draft.Note?.Trim(), ThoiDiem = DateTime.UtcNow };
        db.Xacminhdichvu.Add(item);
        request.TrangThai = draft.Result == "NeedsInfo" ? "NeedsInfo" : "Reviewing";
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(QuotationWriteStatus.Saved, item);
    }

    public async Task<QuotationWrite<int>> IssueAsync(int id, QuoteDraft draft, decimal total, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await db.Yeucautourrieng.SingleOrDefaultAsync(item => item.MaYc == id, ct);
        if (request is null) return new(QuotationWriteStatus.NotFound);
        if (request.TrangThai == "Accepted") return new(QuotationWriteStatus.Conflict);
        if (!await db.Xacminhdichvu.AnyAsync(item => item.MaYc == id && item.KetQua == "Available", ct))
            return new(QuotationWriteStatus.NeedsVerification);
        var old = await db.Baogia.Where(item => item.MaYc == id && item.TrangThai == "Issued").ToListAsync(ct);
        foreach (var item in old) item.TrangThai = "Replaced";
        var quote = new Baogia { MaYc = id, MaPb = request.MaPb, TongGia = total, KhoanCanThu = draft.AmountDue,
            DieuKienHuy = draft.CancellationTerms.Trim(), HanHieuLuc = draft.ExpiresAt.ToUniversalTime(), TrangThai = "Issued" };
        db.Baogia.Add(quote);
        await db.SaveChangesAsync(ct);
        foreach (var line in draft.Lines)
            db.Hangmucbaogia.Add(new Hangmucbaogia { MaBg = quote.MaBg, MoTa = line.Description.Trim(),
                DonViTinh = line.Unit.Trim(), SoLuong = line.Quantity, DonGia = line.UnitPrice });
        request.TrangThai = "Quoted";
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(QuotationWriteStatus.Saved, quote.MaBg);
    }

    public Task<Baogia?> QuoteAsync(int requestId, int quoteId, int accountId, CancellationToken ct) =>
        db.Baogia.AsNoTracking().Include(item => item.Dondat)
            .Include(item => item.MaYcNavigation).ThenInclude(request => request.MaPbNavigation)
            .ThenInclude(version => version.MaLtNavigation)
            .SingleOrDefaultAsync(item => item.MaBg == quoteId && item.MaYc == requestId
                && item.MaYcNavigation.MaPbNavigation.MaLtNavigation.MaTk == accountId, ct);

    public async Task<QuotationWrite<Dondat>> AcceptAsync(int requestId, int quoteId, int accountId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var quote = await db.Baogia.Include(item => item.Dondat)
            .Include(item => item.MaYcNavigation).ThenInclude(request => request.MaPbNavigation).ThenInclude(version => version.MaLtNavigation)
            .SingleOrDefaultAsync(item => item.MaBg == quoteId && item.MaYc == requestId, ct);
        if (quote is null || quote.MaYcNavigation.MaPbNavigation.MaLtNavigation.MaTk != accountId)
            return new(QuotationWriteStatus.NotFound);
        if (quote.Dondat is { } existing) return new(QuotationWriteStatus.Saved, existing);
        if (quote.TrangThai != "Issued" || quote.HanHieuLuc <= DateTime.UtcNow)
            return new(QuotationWriteStatus.Conflict);
        var snapshot = System.Text.Json.JsonSerializer.Deserialize<Contracts.Itinerary>(
            quote.MaYcNavigation.MaPbNavigation.NoiDungJson,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        if (snapshot is null || snapshot.UpdatedAt != quote.MaYcNavigation.MaPbNavigation.MaLtNavigation.NgayCapNhat)
            return new(QuotationWriteStatus.StaleItinerary);
        var order = new Dondat { MaTk = accountId, MaBg = quoteId, SoKhach = quote.MaYcNavigation.SoKhach,
            GiaChot = quote.TongGia, DieuKienHuy = quote.DieuKienHuy, TrangThai = "Pending", NgayDat = DateTime.UtcNow };
        db.Dondat.Add(order);
        quote.TrangThai = "Accepted";
        quote.MaYcNavigation.TrangThai = "Accepted";
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(QuotationWriteStatus.Saved, order);
    }

    public Task<List<Dondat>> OrdersAsync(int accountId, CancellationToken ct) =>
        db.Dondat.AsNoTracking().Where(item => item.MaTk == accountId && item.MaBg != null)
            .OrderByDescending(item => item.NgayDat).ToListAsync(ct);
}
