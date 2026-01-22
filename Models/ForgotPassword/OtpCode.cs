using System.ComponentModel.DataAnnotations;

namespace EcoConnect_Hanoi.Models.ForgotPassword;

public class OtpCode
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Email { get; set; }

    [Required]
    public string Code { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpireAt { get; set; }
    public bool IsUsed { get; set; } = false;
}