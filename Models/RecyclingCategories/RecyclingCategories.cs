using System.ComponentModel.DataAnnotations;

namespace EcoConnect_Hanoi.Models
{
    public class RecyclingCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên loại không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên loại không được vượt quá 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }

        // Hướng dẫn chi tiết cách tái chế (ví dụ: "Rửa sạch, ép dẹt, bỏ vào thùng riêng")
        public string? RecyclingGuide { get; set; }

        // URL ảnh minh họa cho loại rác
        public string? ImageUrl { get; set; }
    }
}