using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Persistence;

// DbContext dùng chung (nợ kỹ thuật, xem ADR-001). Mỗi module đóng góp phần mô hình riêng của mình
// bằng một hàm Apply<Module>Model nằm trong thư mục DataAccess của module đó; chỉ gọi ở đây.
public partial class DongNamTripDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        ApplyAdministrationModel(modelBuilder);
        ApplyBookingModel(modelBuilder);
    }
}
