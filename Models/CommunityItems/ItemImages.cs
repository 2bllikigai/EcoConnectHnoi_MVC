using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoConnect_Hanoi.Models;

public class ItemImages
{
    [Key]
    public int ImageId { get; set; }

    [Required]
    [ForeignKey(nameof(CommunityItem))] // đúng tên thuộc tính
    public int ItemId { get; set; }
    [Required]

    public CommunityItems CommunityItem { get; set; }

    [Required]
    public string ImageUrl { get; set; }
}