using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoConnect_Hanoi.Models;

// Đưa Enum ra ngoài để tránh lỗi CS0426 và dễ gọi từ các View/Controller khác
public enum CategoryType
{
    Clothes,
    Electronics,
    Furniture,
    Book,
    Toy,
    Custom,
}

public class ItemCategory // Chuẩn hóa về tên số ít
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ItemCategoryId { get; set; }

    [Required] 
    public CategoryType Name { get; set; }

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    // Sử dụng CommunityItem (số ít) đã sửa ở bước trước
    public ICollection<CommunityItem> CommunityItems { get; set; } = new List<CommunityItem>();
}