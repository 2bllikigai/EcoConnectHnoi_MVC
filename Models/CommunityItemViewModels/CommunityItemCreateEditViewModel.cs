using System.ComponentModel.DataAnnotations;
using EcoConnect_Hanoi.Models; // Để sử dụng ItemType, ItemCondittions, etc.
using Microsoft.AspNetCore.Http; // Để sử dụng IFormFileCollection

namespace EcoConnect_Hanoi.Models.CommunityItemViewModels
{
    public class CommunityItemCreateEditViewModel
    {
        public int ItemId { get; set; } // Chỉ cần thiết cho Edit

        [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Tiêu đề không được vượt quá 50 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả là bắt buộc.")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Danh mục")]
        [Required(ErrorMessage = "Danh mục là bắt buộc.")]
        public int ItemCategoryId { get; set; }

        [Display(Name = "Tình trạng sản phẩm")]
        [Required(ErrorMessage = "Tình trạng sản phẩm là bắt buộc.")]
        public ItemCondittion ItemCondition { get; set; }

        [Display(Name = "Loại giao dịch")]
        [Required(ErrorMessage = "Loại giao dịch là bắt buộc.")]
        public CommunityItem.ItemType Type { get; set; }

        [Display(Name = "Địa điểm ưu tiên")]
        [MaxLength(100, ErrorMessage = "Địa điểm ưu tiên không được vượt quá 100 ký tự.")]
        public string PreferredLocation { get; set; } = string.Empty;

        [Display(Name = "Mong muốn trao đổi (nếu là trao đổi)")]
        [MaxLength(100, ErrorMessage = "Mong muốn trao đổi không được vượt quá 100 ký tự.")]
        // Không dùng Required ở đây vì nó sẽ được set null nếu là Giveaway
        public string? ExchangeWishes { get; set; } = string.Empty; // Dùng 'string?' để cho phép null

        [Display(Name = "Hình ảnh sản phẩm")]
        // Không dùng Required ở đây, sẽ kiểm tra riêng trong Controller
        public IFormFileCollection? ImageFiles { get; set; } // Cho phép nhiều ảnh, dùng '?' cho phép null
        
        // Thuộc tính để hiển thị các ảnh hiện có trong trường hợp Edit
        public List<ItemImages>? ExistingImages { get; set; }
    }
}