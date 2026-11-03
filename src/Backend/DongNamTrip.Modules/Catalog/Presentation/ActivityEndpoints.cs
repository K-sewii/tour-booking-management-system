using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Lớp giao diện (UI) quản trị hoạt động và suất (SCR15). Không chứa quy tắc nghiệp vụ.</summary>
public static class ActivityEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var staff = endpoints.MapGroup("/api/admin")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor, AppRoles.Operator));

        var activities = staff.MapGroup("/activities");
        activities.MapGet("", (int? destinationId, ActivityService service, CancellationToken ct) => service.ListAsync(destinationId, ct));
        activities.MapPost("", async (ActivityForm form, ClaimsPrincipal user, ActivityService service, CancellationToken ct) =>
            ToHttp(await service.SaveAsync(null, form, AccountId(user), ct))).WithValidation<ActivityForm>();
        activities.MapPut("/{id:int}", async (int id, ActivityForm form, ClaimsPrincipal user, ActivityService service, CancellationToken ct) =>
            ToHttp(await service.SaveAsync(id, form, AccountId(user), ct))).WithValidation<ActivityForm>();
        activities.MapPost("/{id:int}/status", async (int id, bool? confirm, StatusChange change, ClaimsPrincipal user, ActivityService service, CancellationToken ct) =>
            ToHttp(await service.SetActivityStatusAsync(id, change.Status, confirm == true, AccountId(user), ct))).WithValidation<StatusChange>();
        activities.MapPost("/{id:int}/slots", async (int id, SlotForm form, ClaimsPrincipal user, ActivityService service, CancellationToken ct) =>
            ToHttp(await service.CreateSlotAsync(id, form, AccountId(user), ct))).WithValidation<SlotForm>();

        var slots = staff.MapGroup("/slots");
        slots.MapPut("/{id:int}", async (int id, SlotForm form, ClaimsPrincipal user, ActivityService service, CancellationToken ct) =>
            ToHttp(await service.UpdateSlotAsync(id, form, AccountId(user), ct))).WithValidation<SlotForm>();
        slots.MapPost("/{id:int}/status", async (int id, bool? confirm, StatusChange change, ClaimsPrincipal user, ActivityService service, CancellationToken ct) =>
            ToHttp(await service.SetSlotStatusAsync(id, change.Status, confirm == true, AccountId(user), ct))).WithValidation<StatusChange>();
    }
}
