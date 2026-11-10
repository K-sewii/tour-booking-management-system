using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace DongNamTrip.Api;

/// <summary>OpenAPI uses the built-in ASP.NET Core generator; Swagger UI only renders that document.</summary>
public static class OpenApiConfiguration
{
    public static IServiceCollection AddDongNamTripOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "DongNamTrip API";
                document.Info.Version = "v1";
                document.Info.Description = "Development-only API documentation. Bearer credentials are opaque, in-memory session tokens, not JWTs.";
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "opaque session token",
                    Description = "Paste only the token returned by /api/auth/login or /api/auth/register. It is held in memory by the API and expires when the API restarts."
                };
                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                if (metadata?.OfType<IAllowAnonymous>().Any() == true || metadata?.OfType<IAuthorizeData>().Any() != true)
                    return Task.CompletedTask;

                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
                });

                var roles = metadata.OfType<IAuthorizeData>()
                    .SelectMany(data => (data.Roles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (roles.Length > 0)
                    operation.Description = $"{operation.Description}\n\nRequires one of these staff roles: {string.Join(", ", roles)}.".Trim();

                return Task.CompletedTask;
            });
        });
        return services;
    }

    public static WebApplication UseDongNamTripOpenApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.MapOpenApi("/openapi/{documentName}.json");
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "swagger";
            options.SwaggerEndpoint("/openapi/v1.json", "DongNamTrip API v1");
            options.DocumentTitle = "DongNamTrip API — Swagger";
        });
        return app;
    }
}
