using System.ComponentModel.DataAnnotations;

namespace DongNamTrip.Modules;

/// <summary>
/// Kiểm tra DataAnnotations/IValidatableObject của body kiểu <typeparamref name="T"/> trước khi vào handler;
/// sai thì trả 400 dạng ValidationProblem (ProblemDetails). Gắn bằng <see cref="ValidationExtensions.WithValidation{T}"/>.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.OfType<T>().FirstOrDefault() is not { } model)
            return Results.BadRequest();

        var issues = new List<ValidationResult>();
        if (Validator.TryValidateObject(model, new ValidationContext(model), issues, true))
            return await next(context);

        var errors = issues
            .SelectMany(issue => (issue.MemberNames.Any() ? issue.MemberNames : [""]).Select(member => (member, message: issue.ErrorMessage ?? "")))
            .GroupBy(item => item.member)
            .ToDictionary(group => group.Key, group => group.Select(item => item.message).ToArray());
        return Results.ValidationProblem(errors);
    }
}

public static class ValidationExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class =>
        builder.AddEndpointFilter<ValidationFilter<T>>();
}
