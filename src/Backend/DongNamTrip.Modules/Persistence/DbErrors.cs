using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Persistence;

/// <summary>Nhận diện lỗi SQL Server để repository xử lý tranh chấp thay vì trả 500.</summary>
public static class DbErrors
{
    /// <summary>Vi phạm khóa chính hoặc ràng buộc UNIQUE (SQL Server 2601/2627).</summary>
    public static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };

    /// <summary>Vi phạm khóa ngoại (SQL Server 547), ví dụ xóa bản ghi còn được tham chiếu.</summary>
    public static bool IsForeignKeyViolation(Exception exception) =>
        exception.GetBaseException() is SqlException { Number: 547 };
}
