using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using DongNamTrip.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DongNamTrip.Api;
public sealed class DemoSessions
{
    private readonly ConcurrentDictionary<string, (DemoUser User, DateTimeOffset Expires)> sessions = new();
    private static readonly DemoUser[] users = [new(1, "Đức", "admin@dongnamtrip.local", "Admin"), new(2, "Khách trải nghiệm", "khach@dongnamtrip.local", "Customer"), new(3, "Khách thứ hai", "khach2@dongnamtrip.local", "Customer")];
    public LoginResponse? Login(LoginRequest input)
    {
        if (input.Password != "Demo@123")
            return null;
        var user = users.FirstOrDefault(x => x.Email.Equals(input.Email.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user is null)
            return null;
        foreach (var pair in sessions)
            if (pair.Value.Expires <= DateTimeOffset.UtcNow)
                sessions.TryRemove(pair.Key, out _);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        sessions[token] = (user, DateTimeOffset.UtcNow.AddHours(4));
        return new(token, user);
    }

    public DemoUser? Find(string token) => sessions.TryGetValue(token, out var s) && s.Expires > DateTimeOffset.UtcNow ? s.User : null;
    public void Remove(string token) => sessions.TryRemove(token, out _);
}

public sealed class DemoAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, DemoSessions sessions) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());
        var user = sessions.Find(header[7..]);
        if (user is null)
            return Task.FromResult(AuthenticateResult.Fail("Phiên demo hết hạn."));
        var identity = new ClaimsIdentity([new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.Name), new(ClaimTypes.Role, user.Role)], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
