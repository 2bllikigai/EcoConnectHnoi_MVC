using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EcoConnect_Hanoi.Models.Enums;
namespace EcoConnect_Hanoi.Models;


public enum ItemType
{
    Giveaway =1,
    Exchange =2
}

public enum ItemStatus
{
    Available,
    Reserved,
    Completed,
}

public class CommunityItem // Đổi thành số ít để tránh lỗi "Type name exists in type"
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ItemId { get; set; }

    [Required]
    [ForeignKey("User")]
    public int OwnerUserId { get; set; }
    public Users User { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MaxLength(50)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [ForeignKey("ItemCategory")]
    public int ItemCategoryId { get; set; }
    public ItemCategory ItemCategory { get; set; }

    // Gọi trực tiếp enum đã đưa ra ngoài
    public ItemCondittion ItemCondition { get; set; }
    public ItemType Type { get; set; }

    [MaxLength(100)]
    public string PreferredLocation { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string ExchangeWishes { get; set; } = string.Empty;
    public ItemStatus Status { get; set; }

    [Required]
    public ICollection<ItemImages> Images { get; set; } = new List<ItemImages>();
}