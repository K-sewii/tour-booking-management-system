using DongNamTrip.Contracts;
using DongNamTrip.Modules.Administration;
using Xunit;

namespace DongNamTrip.Modules.Tests;

public sealed class AdminSafetyTests
{
    [Theory]
    [InlineData(AccountStatus.Active, true, 1, true)]
    [InlineData(AccountStatus.Active, true, 2, false)]
    [InlineData(AccountStatus.Locked, true, 1, false)]
    [InlineData(AccountStatus.Active, false, 1, false)]
    [InlineData(AccountStatus.Active, true, 0, true)]
    public void LastActiveAdministratorCannotBeRemoved(string status, bool isAdmin, int count, bool expected)
    {
        var roles = isAdmin ? new[] { AppRoles.Admin } : new[] { AppRoles.Customer };
        Assert.Equal(expected, AdminSafety.IsLastActiveAdmin(status, roles, count));
    }
}
