using System.ComponentModel.DataAnnotations;

namespace EcoConnect_Hanoi.Models
{
    public class CollectionSchedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required(ErrorMessage = "Khu vực không được để trống.")]
        [StringLength(200, ErrorMessage = "Khu vực không được vượt quá 200 ký tự.")]
        public string Area { get; set; } = string.Empty; // Ví dụ: "Quận Hoàn Kiếm", "Phường A"

        // Sử dụng chuỗi để linh hoạt hơn với các ngày trong tuần
        [Required(ErrorMessage = "Ngày thu gom không được để trống.")]
        [StringLength(50, ErrorMessage = "Ngày thu gom không được vượt quá 50 ký tự.")]
        // Có thể dùng enum DayOfWeek và HasConversion<string>() trong DbContext
        public string CollectionDay { get; set; } = string.Empty; // Ví dụ: "Thứ Hai", "Thứ Ba", "Thứ Tư, Thứ Sáu"

        [Required(ErrorMessage = "Thời gian thu gom không được để trống.")]
        [StringLength(100, ErrorMessage = "Thời gian thu gom không được vượt quá 100 ký tự.")]
        public string CollectionTime { get; set; } = string.Empty; // Ví dụ: "Sáng: 7h-9h", "Chiều: 14h-16h"

        // Loại rác được thu gom trong ngày này (ví dụ: "Rác hữu cơ", "Rác vô cơ", "Tái chế")
        [StringLength(100, ErrorMessage = "Loại rác không được vượt quá 100 ký tự.")]
        public string? WasteType { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        public string? Notes { get; set; }
    }
}