using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Persistence;

public class AccountFavorite
{
    public int AccountId { get; set; }
    public string Kind { get; set; } = "";
    public int ItemId { get; set; }
    public DateTime SavedAt { get; set; }
}
public class AccountActivity
{
    public long Id { get; set; }
    public int AccountId { get; set; }
    public string Kind { get; set; } = "";
    public int? ItemId { get; set; }
    public string? Query { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class AccountPreference
{
    public int AccountId { get; set; }
    public bool SecurityNotices { get; set; } = true;
}
public class AccountNotice
{
    public long Id { get; set; }
    public int AccountId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
public partial class DongNamTripDbContext
{
    public DbSet<AccountFavorite> AccountFavorites => Set<AccountFavorite>();
    public DbSet<AccountActivity> AccountActivities => Set<AccountActivity>();
    public DbSet<AccountPreference> AccountPreferences => Set<AccountPreference>();
    public DbSet<AccountNotice> AccountNotices => Set<AccountNotice>();

    // Gọi từ DongNamTripDbContext.Modules.cs; phần mô hình riêng của module Administration.
    private static void ApplyAdministrationModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountFavorite>(entity =>
        {
            entity.ToTable("TAIKHOAN_YEUTHICH");
            entity.HasKey(x => new { x.AccountId, x.Kind, x.ItemId });
            entity.Property(x => x.AccountId).HasColumnName("MaTK");
            entity.Property(x => x.Kind).HasColumnName("LoaiDoiTuong").IsUnicode(false).HasMaxLength(12);
            entity.Property(x => x.ItemId).HasColumnName("MaDoiTuong");
            entity.Property(x => x.SavedAt).HasColumnName("NgayLuu");
        });
        modelBuilder.Entity<AccountActivity>(entity =>
        {
            entity.ToTable("TAIKHOAN_LICHSU");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("MaLS");
            entity.Property(x => x.AccountId).HasColumnName("MaTK");
            entity.Property(x => x.Kind).HasColumnName("LoaiDoiTuong").IsUnicode(false).HasMaxLength(12);
            entity.Property(x => x.ItemId).HasColumnName("MaDoiTuong");
            entity.Property(x => x.Query).HasColumnName("TuKhoa").HasMaxLength(200);
            entity.Property(x => x.CreatedAt).HasColumnName("ThoiDiem");
        });
        modelBuilder.Entity<AccountPreference>(entity =>
        {
            entity.ToTable("TAIKHOAN_TUYCHON");
            entity.HasKey(x => x.AccountId);
            entity.Property(x => x.AccountId).HasColumnName("MaTK");
            entity.Property(x => x.SecurityNotices).HasColumnName("NhanTBBaoMat");
        });
        modelBuilder.Entity<AccountNotice>(entity =>
        {
            entity.ToTable("TAIKHOAN_THONGBAO");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("MaTB");
            entity.Property(x => x.AccountId).HasColumnName("MaTK");
            entity.Property(x => x.Title).HasColumnName("TieuDe").HasMaxLength(160);
            entity.Property(x => x.Body).HasColumnName("NoiDung").HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasColumnName("NgayTao");
            entity.Property(x => x.ReadAt).HasColumnName("NgayDoc");
        });
    }
}
