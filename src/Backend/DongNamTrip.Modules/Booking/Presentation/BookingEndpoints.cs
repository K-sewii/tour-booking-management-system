using System.Security.Claims;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Contracts;
using Microsoft.EntityFrameworkCore;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Booking;

/// <summary>Lớp giao diện (UI) của module Booking: ánh xạ HTTP endpoint.</summary>
public static class BookingEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
endpoints.MapGet("/api/account/payments", async (ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken cancellationToken) =>
{
    var owner = AccountId(user);
    var orders = await db.Dondat.AsNoTracking().Where(item => item.MaTk == owner)
        .Include(item => item.Giaodichtien).OrderByDescending(item => item.NgayDat).ToListAsync(cancellationToken);
    return Results.Ok(orders.Select(item => new PaymentEntry(item.MaDon, item.NgayDat, item.TrangThai, item.GiaChot, item.SoKhach, item.MaBg, item.MaLkh,
        item.Giaodichtien.OrderByDescending(entry => entry.ThoiDiem).Select(entry => new MoneyEntry(entry.MaGd, entry.LoaiGd, entry.SoTien, entry.ThoiDiem, entry.MaThamChieu)).ToArray())));
}).RequireAuthorization();
endpoints.MapGet("/api/account/payment-choice", async (ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    var choice = await db.AccountPaymentChoices.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == owner, ct);
    return Results.Ok(new PaymentChoiceDto(choice?.MethodCode, choice?.UpdatedAt));
}).RequireAuthorization();
endpoints.MapPut("/api/account/payment-choice", async (PaymentChoiceRequest input, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    if (input.MethodCode is not ("bank_transfer" or "cash")) return Results.BadRequest();
    var owner = AccountId(user);
    var choice = await db.AccountPaymentChoices.SingleOrDefaultAsync(x => x.AccountId == owner, ct);
    if (choice is null)
    {
        choice = new() { AccountId = owner };
        db.AccountPaymentChoices.Add(choice);
    }
    choice.MethodCode = input.MethodCode;
    choice.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    return Results.Ok(new PaymentChoiceDto(choice.MethodCode, choice.UpdatedAt));
}).RequireAuthorization();
endpoints.MapGet("/api/account/payments/{id:int}", async (int id, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken cancellationToken) =>
{
    var owner = AccountId(user);
    var item = await db.Dondat.AsNoTracking().Include(order => order.Giaodichtien)
        .SingleOrDefaultAsync(order => order.MaDon == id && order.MaTk == owner, cancellationToken);
    return item is null ? Results.NotFound() : Results.Ok(new PaymentEntry(item.MaDon, item.NgayDat, item.TrangThai, item.GiaChot, item.SoKhach, item.MaBg, item.MaLkh,
        item.Giaodichtien.OrderByDescending(entry => entry.ThoiDiem).Select(entry => new MoneyEntry(entry.MaGd, entry.LoaiGd, entry.SoTien, entry.ThoiDiem, entry.MaThamChieu)).ToArray()));
}).RequireAuthorization();
    }
}
