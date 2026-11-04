using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DongNamTrip.Modules;

/// <summary>Hàm dùng chung cho endpoint của các module.</summary>
public static class ModuleHttp
{
    public static bool Valid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), [], true);
    public static int AccountId(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Đổi <see cref="OpResult{T}"/> sang HTTP: 200 kèm giá trị; lỗi thì 400/403/404/409 (kèm giá trị nếu có, ngược lại ProblemDetails.detail).</summary>
    public static IResult ToHttp<T>(OpResult<T> result)
    {
        if (result.Status == OpStatus.Ok)
            return Results.Ok(result.Value);
        var status = result.Status switch
        {
            OpStatus.Invalid => StatusCodes.Status400BadRequest,
            OpStatus.Forbidden => StatusCodes.Status403Forbidden,
            OpStatus.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status409Conflict
        };
        return result.Value is not null
            ? Results.Json(result.Value, statusCode: status)
            : Results.Problem(detail: result.Message, statusCode: status);
    }
}
