using EcoConnect_Hanoi.Models;
using EcoConnect_Hanoi.Models.ForgotPassword;
using Microsoft.EntityFrameworkCore;

namespace EcoConnect_Hanoi.Data;

public class EcoConnectHnContext : DbContext
{
    
    public EcoConnectHnContext(DbContextOptions<EcoConnectHnContext> options, ILogger<EcoConnectHnContext> logger) : base(options){}
    public DbSet<Users> Users { get; set; }
    public DbSet<CommunityItems> CommunityItems { get; set; }
    public DbSet<ItemCategories> ItemCategories { get; set; }
    public DbSet<ItemImages> ItemImages { get; set; }
    public DbSet<OtpCode> OtpCodes { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình khóa chính cho Users
        modelBuilder.Entity<Users>()
            .HasKey(u => u.UserId);

        // Unique email
        modelBuilder.Entity<Users>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Quan hệ Users - CommunityItems (1 - nhiều)
        modelBuilder.Entity<CommunityItems>()
            .HasOne(ci => ci.User)
            .WithMany(u => u.CommunityItems)
            .HasForeignKey(ci => ci.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quan hệ ItemCategory - CommunityItems (1 - nhiều)
        modelBuilder.Entity<CommunityItems>()
            .HasOne(ci => ci.ItemCategory)
            .WithMany(cat => cat.CommunityItems)
            .HasForeignKey(ci => ci.ItemCategoryId);

        // Nếu bạn có ItemImages:
        modelBuilder.Entity<ItemImages>()
            .HasOne(img => img.CommunityItem)
            .WithMany(ci => ci.Images)
            .HasForeignKey(img => img.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }

}

