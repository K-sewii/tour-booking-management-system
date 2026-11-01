using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DongNamTrip.Modules;

/// <summary>Hàm dùng chung cho endpoint của các module.</summary>
public static class ModuleHttp
{
    public static bool Valid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), [], true);
    public static int AccountId(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
