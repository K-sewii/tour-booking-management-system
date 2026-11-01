namespace DongNamTrip.Api.Modules;

/// <summary>Điểm vào của một module nghiệp vụ; Program.cs chỉ gọi giao diện này.</summary>
public interface IModule
{
    void Register(IServiceCollection services, IConfiguration configuration);
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
