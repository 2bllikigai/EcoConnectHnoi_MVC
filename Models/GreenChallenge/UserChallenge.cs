using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoConnect_Hanoi.Models
{
    public class UserChallenge
    {
        [Key]
        public int UserChallengeId { get; set; }

        [Required(ErrorMessage = "Người dùng không được để trống.")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public Users User { get; set; } = default!; // Navigation property

        [Required(ErrorMessage = "Thử thách không được để trống.")]
        public int ChallengeId { get; set; }
        [ForeignKey("ChallengeId")]
        public Challenge Challenge { get; set; } = default!; // Navigation property

        [Required(ErrorMessage = "Ngày tham gia không được để trống.")]
        public DateTime EnrollmentDate { get; set; }

        public DateTime? CompletionDate { get; set; } // Ngày hoàn thành (null nếu chưa hoàn thành)

        [Required(ErrorMessage = "Tiến độ không được để trống.")]
        [Range(0, 100, ErrorMessage = "Tiến độ phải từ 0 đến 100.")]
        public int Progress { get; set; } // Phần trăm hoàn thành (0-100)

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        public UserChallengeStatus Status { get; set; } = UserChallengeStatus.Enrolled; // Trạng thái tham gia của người dùng
    }

    // Enum cho trạng thái tham gia thử thách của người dùng
    public enum UserChallengeStatus
    {
        Enrolled,   // Đã đăng ký
        InProgress, // Đang thực hiện
        Completed,  // Đã hoàn thành
        Failed      // Thất bại (quá hạn mà chưa hoàn thành)
    }
}