using System.ComponentModel.DataAnnotations;

namespace EcoConnect_Hanoi.Models
{
    public class Challenge
    {
        [Key]
        public int ChallengeId { get; set; }

        [Required(ErrorMessage = "Tiêu đề thử thách không được để trống.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả thử thách không được để trống.")]
        public string Description { get; set; } = string.Empty; // Có thể dài hơn

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống.")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Điểm thưởng không được để trống.")]
        [Range(0, int.MaxValue, ErrorMessage = "Điểm thưởng phải là số không âm.")]
        public int RewardPoints { get; set; }

        public string? ImageUrl { get; set; } // URL ảnh minh họa cho thử thách

        [Required(ErrorMessage = "Trạng thái thử thách không được để trống.")]
        public ChallengeStatus Status { get; set; } = ChallengeStatus.Upcoming; // Trạng thái của thử thách
    }

    // Enum cho trạng thái thử thách
    public enum ChallengeStatus
    {
        Upcoming = 0,
        Active = 1,
        Completed = 2,
        Cancelled = 3
    }

}