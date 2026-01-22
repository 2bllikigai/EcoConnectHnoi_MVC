using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoConnect_Hanoi.Models
{
    public class UserCollectionRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required(ErrorMessage = "Người dùng yêu cầu không được để trống.")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public Users User { get; set; } = default!; // Navigation property

        [Required(ErrorMessage = "Ngày yêu cầu không được để trống.")]
        public DateTime RequestDate { get; set; }

        [Required(ErrorMessage = "Ngày muốn thu gom không được để trống.")]
        public DateTime PreferredDate { get; set; }

        [Required(ErrorMessage = "Mô tả rác thải không được để trống.")]
        [StringLength(1000, ErrorMessage = "Mô tả rác thải không được vượt quá 1000 ký tự.")]
        public string WasteDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        public RequestStatus Status { get; set; } = RequestStatus.Pending; // Trạng thái của yêu cầu
    }

    // Enum cho trạng thái yêu cầu thu gom
    public enum RequestStatus
    {
        Pending,   // Đang chờ xử lý
        Approved,  // Đã được chấp thuận
        Completed, // Đã hoàn thành
        Rejected   // Bị từ chối
    }
}