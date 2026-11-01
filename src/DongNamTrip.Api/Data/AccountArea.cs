using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Api.Data;

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
public class AccountPaymentChoice
{
    public int AccountId { get; set; }
    public string MethodCode { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}

public partial class DongNamTripDbContext
{
    public DbSet<AccountFavorite> AccountFavorites => Set<AccountFavorite>();
    public DbSet<AccountActivity> AccountActivities => Set<AccountActivity>();
    public DbSet<AccountPreference> AccountPreferences => Set<AccountPreference>();
    public DbSet<AccountNotice> AccountNotices => Set<AccountNotice>();
    public DbSet<AccountPaymentChoice> AccountPaymentChoices => Set<AccountPaymentChoice>();

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountFavorite>(entity => { entity.ToTable("ACCOUNT_FAVORITE"); entity.HasKey(x => new { x.AccountId, x.Kind, x.ItemId }); entity.Property(x => x.Kind).IsUnicode(false).HasMaxLength(12); });
        modelBuilder.Entity<AccountActivity>(entity => { entity.ToTable("ACCOUNT_ACTIVITY"); entity.HasKey(x => x.Id); entity.Property(x => x.Kind).IsUnicode(false).HasMaxLength(12); entity.Property(x => x.Query).HasMaxLength(200); });
        modelBuilder.Entity<AccountPreference>(entity => { entity.ToTable("ACCOUNT_PREFERENCE"); entity.HasKey(x => x.AccountId); });
        modelBuilder.Entity<AccountNotice>(entity => { entity.ToTable("ACCOUNT_NOTICE"); entity.HasKey(x => x.Id); entity.Property(x => x.Title).HasMaxLength(160); entity.Property(x => x.Body).HasMaxLength(500); });
        modelBuilder.Entity<AccountPaymentChoice>(entity => { entity.ToTable("ACCOUNT_PAYMENT_CHOICE"); entity.HasKey(x => x.AccountId); entity.Property(x => x.MethodCode).IsUnicode(false).HasMaxLength(20); });
    }
}
