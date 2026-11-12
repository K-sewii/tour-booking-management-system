using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DongNamTrip.Modules;

/// <summary>Hàm dùng chung cho endpoint của các module.</summary>
public static class ModuleHttp
{
    public static bool Valid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), [], true);
    public static int AccountId(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Đổi <see cref="OpResult{T}"/> sang HTTP: 200 kèm giá trị; lỗi thì 400/403/404/409/422 (kèm giá trị nếu có, ngược lại ProblemDetails.detail).</summary>
    public static IResult ToHttp<T>(OpResult<T> result) => ToHttp(result, value => Results.Ok(value));

    /// <summary>Như <see cref="ToHttp{T}(OpResult{T})"/> nhưng cho phép tự chọn phản hồi thành công (ví dụ 201).</summary>
    public static IResult ToHttp<T>(OpResult<T> result, Func<T, IResult> onOk)
    {
        if (result.Status == OpStatus.Ok)
            return onOk(result.Value!);
        var status = result.Status switch
        {
            OpStatus.Invalid => StatusCodes.Status400BadRequest,
            OpStatus.Forbidden => StatusCodes.Status403Forbidden,
            OpStatus.NotFound => StatusCodes.Status404NotFound,
            OpStatus.Unprocessable => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status409Conflict
        };
        return result.Value is not null
            ? Results.Json(result.Value, statusCode: status)
            : Results.Problem(detail: result.Message, statusCode: status);
    }

    /// <summary>201 Created khi vừa tạo, 200 khi trả bản ghi đã có.</summary>
    public static IResult ToHttpFresh<T>(OpResult<Fresh<T>> result, Func<T, string>? location = null) =>
        ToHttp(result, fresh => fresh.IsNew
            ? (location is null ? Results.Json(fresh.Value, statusCode: StatusCodes.Status201Created) : Results.Created(location(fresh.Value), fresh.Value))
            : Results.Ok(fresh.Value));
}
