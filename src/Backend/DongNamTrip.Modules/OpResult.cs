namespace DongNamTrip.Modules;

/// <summary>Kết quả nghiệp vụ để Presentation đổi sang HTTP: Ok, Invalid (400), Forbidden (403), NotFound (404), Conflict (409).</summary>
public enum OpStatus { Ok, Invalid, Forbidden, NotFound, Conflict }

/// <summary>Thất bại chưa gắn kiểu giá trị; đổi ngầm sang <see cref="OpResult{T}"/> (xem <see cref="Op"/>).</summary>
public readonly record struct OpFailure(OpStatus Status, string Message);

/// <summary>
/// Kết quả của một hàm Service. Khi <see cref="Status"/> khác Ok, <see cref="Value"/> có thể vẫn mang dữ liệu
/// để client xử lý (ví dụ cần xác nhận trước khi tạm ngừng); Message là lời giải thích cho người dùng.
/// </summary>
public sealed record OpResult<T>(T? Value, OpStatus Status, string? Message = null)
{
    public bool IsOk => Status == OpStatus.Ok;

    public static implicit operator OpResult<T>(OpFailure failure) => new(default, failure.Status, failure.Message);
}

public static class Op
{
    public static OpResult<T> Ok<T>(T value) => new(value, OpStatus.Ok);
    public static OpFailure Invalid(string message) => new(OpStatus.Invalid, message);
    public static OpFailure Forbidden(string message) => new(OpStatus.Forbidden, message);
    public static OpFailure NotFound(string message) => new(OpStatus.NotFound, message);
    public static OpFailure Conflict(string message) => new(OpStatus.Conflict, message);

    /// <summary>Xung đột nhưng vẫn trả dữ liệu cho client (ví dụ yêu cầu xác nhận).</summary>
    public static OpResult<T> Conflict<T>(string message, T value) => new(value, OpStatus.Conflict, message);
}
