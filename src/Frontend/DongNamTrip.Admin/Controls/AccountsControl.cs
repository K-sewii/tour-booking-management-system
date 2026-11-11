using DongNamTrip.Client;

namespace DongNamTrip.Admin;

public sealed class AccountsControl : UserControl
{
    public AccountsControl(TripApiClient api, string token)
    {
        Controls.Add(new AccountManagementControl(api, token) { Dock = DockStyle.Fill });
    }
}
