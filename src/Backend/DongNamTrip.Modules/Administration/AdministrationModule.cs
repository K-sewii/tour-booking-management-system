using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace DongNamTrip.Modules.Administration;

public sealed class AdministrationModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SqlAuthenticationOptions>(configuration.GetSection(SqlAuthenticationOptions.SectionName));
        services.AddSingleton<SqlSessionStore>();
        services.AddSingleton<IPasswordHasher<Taikhoan>, PasswordHasher<Taikhoan>>();
        services.AddScoped<SqlAuthService>();
        services.AddScoped<AccountAreaRepository>();
        services.AddScoped<AccountAreaService>();
        services.AddScoped<AuditRepository>();
        services.AddScoped<AuditService>();
        services.AddScoped<IAdministrationApi>(sp => sp.GetRequiredService<AuditService>());
        services.AddSingleton<DevelopmentAdminBootstrapper>();
        services.AddSingleton<DevelopmentTestAccountsBootstrapper>();
        services.AddAuthentication("SqlBearer").AddScheme<AuthenticationSchemeOptions, SqlAuthenticationHandler>("SqlBearer", _ => { });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => AdministrationEndpoints.Map(endpoints);

    public static async Task EnsureDevelopmentAccountsAsync(IServiceProvider services, IHostEnvironment environment)
    {
        await services.GetRequiredService<DevelopmentAdminBootstrapper>().EnsureCreatedAsync(environment);
        await services.GetRequiredService<DevelopmentTestAccountsBootstrapper>().EnsureCreatedAsync(environment);
    }
}
