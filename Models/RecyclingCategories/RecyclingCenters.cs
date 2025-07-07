using System.ComponentModel.DataAnnotations;

namespace EcoConnect_Hanoi.Models
{
    public class RecyclingCenter
    {
        [Key]
        public int CenterId { get; set; }

        [Required(ErrorMessage = "Tên trung tâm không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên trung tâm không được vượt quá 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vĩ độ không được để trống.")]
        public decimal Latitude { get; set; } // Vĩ độ (ví dụ: dùng cho bản đồ)

        [Required(ErrorMessage = "Kinh độ không được để trống.")]
        public decimal Longitude { get; set; } // Kinh độ (ví dụ: dùng cho bản đồ)

        [StringLength(200, ErrorMessage = "Giờ mở cửa không được vượt quá 200 ký tự.")]
        public string? OpeningHours { get; set; }

        // Các loại vật liệu được chấp nhận (ví dụ: "Giấy, Nhựa, Kim loại" hoặc JSON string của List<string>)
        public string? AcceptedMaterials { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        public string? Notes { get; set; }
    }
}