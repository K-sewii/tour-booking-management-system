using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Narration;

/// <summary>Lớp giao diện (UI) biên tập thuyết minh (SCR20), nghe thuyết minh (SCR11) và quyền nghe nội bộ. Không chứa quy tắc nghiệp vụ.</summary>
public static class NarrationEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        MapEditing(endpoints.MapGroup("/api/admin").WithTags("Narration"));
        MapListening(endpoints.MapGroup("/api/listen").WithTags("Narration").RequireAuthorization());
        MapRights(endpoints.MapGroup("/api/internal/listen-rights")
            .WithTags("Narration")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Operator)));
    }

    private static bool CanPublish(ClaimsPrincipal user) => user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.Publisher);

    private static void MapEditing(RouteGroupBuilder admin)
    {
        // Xem: Admin, Editor, Publisher.
        var viewers = admin.MapGroup("").RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor, AppRoles.Publisher));
        viewers.MapGet("/languages", (NarrationService service, CancellationToken ct) => service.LanguagesAsync(ct));
        viewers.MapGet("/narrations", (NarrationService service, CancellationToken ct) => service.ListAsync(ct));
        viewers.MapGet("/audio/{id:int}/file", async (int id, NarrationService service, CancellationToken ct) =>
            await service.AudioPathAsync(id, ct) is { IsOk: true, Value: { } path } ? Results.File(path, "audio/wav", enableRangeProcessing: true) : Results.NotFound());

        // Soạn, dịch, tạo audio: Admin, Editor. Publisher chỉ duyệt và công bố.
        var editors = admin.MapGroup("").RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor));
        editors.MapPost("/narrations", async (NarrationForm form, ClaimsPrincipal user, NarrationService service, CancellationToken ct) =>
            ToHttp(await service.SaveAsync(null, form, AccountId(user), ct))).WithValidation<NarrationForm>();
        editors.MapPut("/narrations/{id:int}", async (int id, NarrationForm form, ClaimsPrincipal user, NarrationService service, CancellationToken ct) =>
            ToHttp(await service.SaveAsync(id, form, AccountId(user), ct))).WithValidation<NarrationForm>();
        editors.MapPost("/narrations/{id:int}/versions", async (int id, NarrationForm form, ClaimsPrincipal user, NarrationService service, CancellationToken ct) =>
            ToHttp(await service.NewVersionAsync(id, form.Source, AccountId(user), ct))).WithValidation<NarrationForm>();
        editors.MapPut("/narrations/{id:int}/translations", async (int id, TranslationForm form, ClaimsPrincipal user, NarrationService service, CancellationToken ct) =>
            ToHttp(await service.SaveTranslationAsync(id, form, AccountId(user), ct))).WithValidation<TranslationForm>();
        editors.MapPost("/translations/{id:int}/audio", async (int id, AudioUpload upload, ClaimsPrincipal user, NarrationService service, CancellationToken ct) =>
            ToHttp(await service.SaveAudioAsync(id, upload, AccountId(user), ct))).WithValidation<AudioUpload>();

        // Đổi trạng thái: gửi duyệt (Editor) hoặc duyệt/công bố (Publisher, Admin); service kiểm tra từng bước chuyển.
        viewers.MapPost("/narrations/{id:int}/status", async (int id, StatusChange change, ClaimsPrincipal user, NarrationService service, CancellationToken ct) =>
            ToHttp(await service.SetStatusAsync(id, change.Status, CanPublish(user), AccountId(user), ct))).WithValidation<StatusChange>();
        viewers.MapPost("/translations/{id:int}/status", async (int id, StatusChange change, ClaimsPrincipal user, NarrationService service, CancellationToken ct) =>
            ToHttp(await service.SetTranslationStatusAsync(id, change.Status, CanPublish(user), AccountId(user), ct))).WithValidation<StatusChange>();
        viewers.MapPost("/audio/{id:int}/status", async (int id, StatusChange change, ClaimsPrincipal user, NarrationService service, CancellationToken ct) =>
            ToHttp(await service.SetAudioStatusAsync(id, change.Status, CanPublish(user), AccountId(user), ct))).WithValidation<StatusChange>();
    }

    private static void MapListening(RouteGroupBuilder listen)
    {
        listen.MapGet("", (ClaimsPrincipal user, ListenService service, CancellationToken ct) => service.CatalogAsync(AccountId(user), ct));
        listen.MapGet("/audio/{id:int}", async (int id, ClaimsPrincipal user, ListenService service, CancellationToken ct) =>
        {
            var result = await service.FileAsync(id, AccountId(user), ct);
            return result is { IsOk: true, Value: { } path } ? Results.File(path, "audio/wav", enableRangeProcessing: true) : ToHttp(result);
        });
    }

    private static void MapRights(RouteGroupBuilder rights)
    {
        rights.MapPost("", async (ListenRightGrant grant, ClaimsPrincipal user, ListenService service, CancellationToken ct) =>
            ToHttp(await service.GrantListenRightAsync(grant, AccountId(user), ct))).WithValidation<ListenRightGrant>();
        rights.MapDelete("/{orderId:int}", async (int orderId, ClaimsPrincipal user, ListenService service, CancellationToken ct) =>
        {
            await service.RevokeListenRightAsync(orderId, AccountId(user), ct);
            return Results.NoContent();
        });
    }
}
