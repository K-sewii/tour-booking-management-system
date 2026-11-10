using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Persistence;

public class AccountPaymentChoice
{
    public int AccountId { get; set; }
    public string MethodCode { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}

public partial class DongNamTripDbContext
{
    public DbSet<AccountPaymentChoice> AccountPaymentChoices => Set<AccountPaymentChoice>();

    // Gọi từ DongNamTripDbContext.Modules.cs; phần mô hình riêng của module Booking.
    private static void ApplyBookingModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountPaymentChoice>(entity =>
        {
            entity.ToTable("TAIKHOAN_THANHTOAN");
            entity.HasKey(x => x.AccountId);
            entity.Property(x => x.AccountId).HasColumnName("MaTK");
            entity.Property(x => x.MethodCode).HasColumnName("PhuongThuc").IsUnicode(false).HasMaxLength(20);
            entity.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
        });
    }
}
