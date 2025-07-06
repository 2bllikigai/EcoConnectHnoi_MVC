using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoConnect_Hanoi.Models;

public class ItemCategories
{
    public enum CategoryType
    {
        Clothes,
        Electronics,
        Furniture,
        Book,
        Toy,
        Custom,
        
    }
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ItemCategoryId { get; set; }

    [Required] public CategoryType Name { get; set; }
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; }

    public ICollection<CommunityItems> CommunityItems { get; set; } = new List<CommunityItems>();


}