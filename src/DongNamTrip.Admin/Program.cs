using DongNamTrip.Client;

namespace DongNamTrip.Admin;
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        var baseUrl = Environment.GetEnvironmentVariable("DONGNAMTRIP_API_URL") ?? "http://localhost:5080/";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || !uri.IsLoopback)
        {
            MessageBox.Show("Bản demo chỉ cho phép API trên localhost. Kiểm tra DONGNAMTRIP_API_URL.");
            return;
        }

        using var http = new HttpClient
        {
            BaseAddress = uri,
            Timeout = TimeSpan.FromSeconds(10)
        };
        var api = new TripApiClient(http);
        using var login = new LoginForm(api);
        if (login.ShowDialog() == DialogResult.OK && login.Session is { } session)
            Application.Run(new MainForm(api, session));
    }
}
