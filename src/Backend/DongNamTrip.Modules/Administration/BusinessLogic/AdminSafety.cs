using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Administration;

/// <summary>Rules for retaining at least one active administrator.</summary>
public static class AdminSafety
{
    public static bool IsLastActiveAdmin(string status, IReadOnlyCollection<string> roles, int activeAdmins) =>
        status == AccountStatus.Active && roles.Contains(AppRoles.Admin) && activeAdmins <= 1;
}
