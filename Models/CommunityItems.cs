using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoConnect_Hanoi.Models;

public class CommunityItems
{
    public enum ItemCondittions
    {
        New,
        LikeNew,
        UsedGood, 
        UsedFair,
    }

    public enum ItemType
    {
        Giveaway,
        Exchange
    }

    public enum ItemStatus
    {
        Available,
        Reserved,
        Completed,
    }
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
    public ItemCategories ItemCategory { get; set; }
    public ItemCondittions ItemCondition { get; set; }
    public ItemType Type { get; set; }
    [MaxLength(100)]
    public string PreferredLocation { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // [Required(ErrorMessage = "ExchangeWishes is required")]
    public string ExchangeWishes { get; set; } = string.Empty;
    public ItemStatus Status { get; set; }
    [Required]
    public ICollection<ItemImages> Images { get; set; } = new List<ItemImages>();


}