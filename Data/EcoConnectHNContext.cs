using EcoConnect_Hanoi.Models;
using EcoConnect_Hanoi.Models.ForgotPassword;
using Microsoft.EntityFrameworkCore;

namespace EcoConnect_Hanoi.Data;

public class EcoConnectHnContext : DbContext
{
    public EcoConnectHnContext(
        DbContextOptions<EcoConnectHnContext> options,
        ILogger<EcoConnectHnContext> logger
    ) : base(options) { }

    public DbSet<Users> Users { get; set; }

    // ✅ GIỮ MODEL SỐ ÍT – DbSet có thể số nhiều (chuẩn EF)
    public DbSet<CommunityItem> CommunityItems { get; set; }
    public DbSet<ItemCategory> ItemCategories { get; set; }

    public DbSet<ItemImages> ItemImages { get; set; }
    public DbSet<OtpCode> OtpCodes { get; set; }

    public DbSet<RecyclingCategory> RecyclingCategories { get; set; }
    public DbSet<RecyclingCenter>? RecyclingCenters { get; set; }

    public DbSet<CollectionSchedule> CollectionSchedules { get; set; }
    public DbSet<UserCollectionRequest>? UserCollectionRequests { get; set; }

    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<UserChallenge> UserChallenges { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OtpCode>().ToTable("OtpCodes");

        // ===== USERS =====
        modelBuilder.Entity<Users>()
            .HasKey(u => u.UserId);

        modelBuilder.Entity<Users>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // ===== COMMUNITY ITEM =====
        modelBuilder.Entity<CommunityItem>()
            .HasOne(ci => ci.User)
            .WithMany(u => u.CommunityItems)
            .HasForeignKey(ci => ci.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ✅ SỬA ĐÚNG: CommunityItem (số ít)
        modelBuilder.Entity<CommunityItem>()
            .HasOne(ci => ci.ItemCategory)
            .WithMany(cat => cat.CommunityItems)
            .HasForeignKey(ci => ci.ItemCategoryId);

        // ===== ITEM IMAGES =====
        modelBuilder.Entity<ItemImages>()
            .HasOne(img => img.CommunityItem)
            .WithMany(ci => ci.Images)
            .HasForeignKey(img => img.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== RECYCLING =====
        modelBuilder.Entity<RecyclingCategory>()
            .HasKey(rc => rc.CategoryId);

        modelBuilder.Entity<RecyclingCenter>()
            .HasKey(rc => rc.CenterId);

        modelBuilder.Entity<CollectionSchedule>()
            .HasKey(cs => cs.ScheduleId);

        // ===== USER COLLECTION REQUEST =====
        modelBuilder.Entity<UserCollectionRequest>()
            .HasKey(ucr => ucr.RequestId);

        modelBuilder.Entity<UserCollectionRequest>()
            .HasOne(ucr => ucr.User)
            .WithMany()
            .HasForeignKey(ucr => ucr.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserCollectionRequest>()
            .Property(ucr => ucr.Status)
            .HasConversion<string>();

        // ===== CHALLENGE =====
        modelBuilder.Entity<Challenge>()
            .HasKey(c => c.ChallengeId);

        modelBuilder.Entity<Challenge>()
            .Property(c => c.Status)
            .HasConversion<string>();

        modelBuilder.Entity<UserChallenge>()
            .HasKey(uc => uc.UserChallengeId);

        modelBuilder.Entity<UserChallenge>()
            .HasOne(uc => uc.User)
            .WithMany()
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserChallenge>()
            .HasOne(uc => uc.Challenge)
            .WithMany()
            .HasForeignKey(uc => uc.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserChallenge>()
            .Property(uc => uc.Status)
            .HasConversion<string>();
    }
}
