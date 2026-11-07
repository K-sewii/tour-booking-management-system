using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Lớp giao diện (UI) quản trị địa điểm (SCR14). Không chứa quy tắc nghiệp vụ.</summary>
public static class PlaceAdminEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var places = endpoints.MapGroup("/api/admin/destinations")
            .WithTags("Catalog")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor));
        places.MapGet("", (PlaceAdminService service, CancellationToken ct) => service.ListAsync(ct));
        places.MapPost("", async (DestinationForm form, ClaimsPrincipal user, PlaceAdminService service, CancellationToken ct) =>
            ToHttp(await service.SaveAsync(null, form, AccountId(user), ct))).WithValidation<DestinationForm>();
        places.MapPut("/{id:int}", async (int id, DestinationForm form, ClaimsPrincipal user, PlaceAdminService service, CancellationToken ct) =>
            ToHttp(await service.SaveAsync(id, form, AccountId(user), ct))).WithValidation<DestinationForm>();
        places.MapPost("/{id:int}/status", async (int id, StatusChange change, ClaimsPrincipal user, PlaceAdminService service, CancellationToken ct) =>
            ToHttp(await service.SetStatusAsync(id, change.Status, AccountId(user), ct))).WithValidation<StatusChange>();
        places.MapDelete("/{id:int}", async (int id, ClaimsPrincipal user, PlaceAdminService service, CancellationToken ct) =>
            ToHttp(await service.DeleteAsync(id, AccountId(user), ct)));

        // Ô chọn địa điểm ở SCR15 và SCR20: Operator/Publisher không có quyền xem danh sách quản trị đầy đủ.
        endpoints.MapGroup("/api/admin/destination-options")
            .WithTags("Catalog")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor, AppRoles.Operator, AppRoles.Publisher))
            .MapGet("", (PlaceAdminService service, CancellationToken ct) => service.OptionsAsync(ct));
    }
}
