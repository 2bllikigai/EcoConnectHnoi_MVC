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
    public DbSet<RecyclingCategory> RecyclingCategories { get; set; }
    public DbSet<RecyclingCenter>? RecyclingCenters { get; set; } // Nếu bạn dùng
    public DbSet<CollectionSchedule> CollectionSchedules { get; set; }
    public DbSet<UserCollectionRequest>? UserCollectionRequests { get; set; } // Nullable nếu không chắc chắn dùng ngay

    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<UserChallenge> UserChallenges { get; set; }


    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OtpCode>().ToTable("OtpCodes");

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
                    // RecyclingCategory
            modelBuilder.Entity<RecyclingCategory>()
                .HasKey(rc => rc.CategoryId);

            // RecyclingCenter (Tùy chọn)
            modelBuilder.Entity<RecyclingCenter>()
                .HasKey(rc => rc.CenterId);

            // CollectionSchedule
            modelBuilder.Entity<CollectionSchedule>()
                .HasKey(cs => cs.ScheduleId);
            // Nếu bạn dùng enum DayOfWeek cho CollectionDay, thì cần HasConversion<string>()
            // modelBuilder.Entity<CollectionSchedule>()
            //     .Property(cs => cs.CollectionDay)
            //     .HasConversion<string>();


            // UserCollectionRequest (Tùy chọn)
            modelBuilder.Entity<UserCollectionRequest>()
                .HasKey(ucr => ucr.RequestId);
            modelBuilder.Entity<UserCollectionRequest>()
                .HasOne(ucr => ucr.User)
                .WithMany() // Nếu Users không có ICollection<UserCollectionRequest>
                .HasForeignKey(ucr => ucr.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Hoặc Cascade nếu muốn xóa request khi user bị xóa

            modelBuilder.Entity<UserCollectionRequest>()
                .Property(ucr => ucr.Status)
                .HasConversion<string>(); // Lưu enum RequestStatus dưới dạng string


            // Challenge
            modelBuilder.Entity<Challenge>()
                .HasKey(c => c.ChallengeId);
            modelBuilder.Entity<Challenge>()
                .Property(c => c.Status)
                .HasConversion<string>(); // Lưu enum ChallengeStatus dưới dạng string


            // UserChallenge
            modelBuilder.Entity<UserChallenge>()
                .HasKey(uc => uc.UserChallengeId);

            modelBuilder.Entity<UserChallenge>()
                .HasOne(uc => uc.User)
                .WithMany() // Nếu Users không có ICollection<UserChallenge>
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Khi User bị xóa, các UserChallenge của họ cũng bị xóa

            modelBuilder.Entity<UserChallenge>()
                .HasOne(uc => uc.Challenge)
                .WithMany() // Nếu Challenge không có ICollection<UserChallenge>
                .HasForeignKey(uc => uc.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade); // Khi Challenge bị xóa, các UserChallenge liên quan cũng bị xóa

            modelBuilder.Entity<UserChallenge>()
                .Property(uc => uc.Status)
                .HasConversion<string>(); // Lưu enum UserChallengeStatus dưới dạng string

            // Điều này là quan trọng nếu bạn có một ICollection trong model User
            // modelBuilder.Entity<Users>()
            //     .HasMany(u => u.UserChallenges)
            //     .WithOne(uc => uc.User)
            //     .HasForeignKey(uc => uc.UserId)
            //     .OnDelete(DeleteBehavior.Cascade);

            // modelBuilder.Entity<Challenge>()
            //     .HasMany(c => c.UserChallenges)
            //     .WithOne(uc => uc.Challenge)
            //     .HasForeignKey(uc => uc.ChallengeId)
            //     .OnDelete(DeleteBehavior.Cascade);
        }
    }


