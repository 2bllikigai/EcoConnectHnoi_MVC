// EcoConnect_Hanoi.Models/Users.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoConnect_Hanoi.Models
{
    // Enum cho vai trò người dùng (Roles)
    public enum UserRole
    {
        User,  // Người dùng thông thường
        Admin  // Quản trị viên (sẽ có toàn quyền quản lý người dùng)
    }

    // Enum cho trạng thái tài khoản (Account Status) - Giữ nguyên
    public enum AccountStatus
    {
        Active,       // Hoạt động bình thường
        Suspended,    // Tạm ngừng (ví dụ: vi phạm chính sách)
        Pending,      // Chờ xác nhận (ví dụ: xác minh email)
        Deactivated   // Vô hiệu hóa (ví dụ: người dùng yêu cầu xóa tài khoản)
    }

    public class Users
    {
        public Users() { }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [MaxLength(255)]
        public string? ProfileImageUrl { get; set; }

        [Required(ErrorMessage = "Họ là bắt buộc.")]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên là bắt buộc.")]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ.")]
        [MaxLength(100)]
        [ConcurrencyCheck]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        [MaxLength(255)]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; } = string.Empty;

        [NotMapped]
        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare("PasswordHash", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        public ICollection<CommunityItems> CommunityItems { get; set; } = new List<CommunityItems>();

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public DateTime? LastLoginDate { get; set; }

        public AccountStatus AccountStatus { get; set; } = AccountStatus.Pending;

        public UserRole Role { get; set; } = UserRole.User; // Mặc định là User

        public bool IsEmailVerified { get; set; } = false;

        [MaxLength(500)]
        public string? EnvironmentalInterests { get; set; }

        public int ActivityScore { get; private set; } = 0;

        [MaxLength(100)]
        public string? LivingArea { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PreferredLanguage { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Điểm Eco phải lớn hơn hoặc bằng 0.")]
        public int EcoPoints { get; private set; } = 0;

        public void AddEcopoints(int points)
        {
            if (points < 0)
                throw new ArgumentOutOfRangeException(nameof(points), "Điểm Eco phải lớn hơn hoặc bằng 0.");
            EcoPoints += points;
        }

        public void ResetEcopoints() => EcoPoints = 0;

        public void AddActivityScore(int score)
        {
            if (score < 0)
                throw new ArgumentOutOfRangeException(nameof(score), "Điểm hoạt động phải lớn hơn hoặc bằng 0.");
            ActivityScore += score;
        }

        public void ResetActivityScore() => ActivityScore = 0;

        public void ActivateAccount()
        {
            if (AccountStatus == AccountStatus.Pending)
            {
                AccountStatus = AccountStatus.Active;
                IsEmailVerified = true;
            }
        }

        public void SuspendAccount()
        {
            if (AccountStatus == AccountStatus.Active)
            {
                AccountStatus = AccountStatus.Suspended;
            }
        }

        public void ReactivateAccount()
        {
            if (AccountStatus == AccountStatus.Suspended)
            {
                AccountStatus = AccountStatus.Active;
            }
        }
    }
}