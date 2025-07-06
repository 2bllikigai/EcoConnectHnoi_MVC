using System.ComponentModel.DataAnnotations;

namespace EcoConnect_Hanoi.Models.ForgotPassword;

public class VerifyOtpViewModel
{
    [Required]
    public string Email { get; set; }

    [Required]
    [MaxLength(6)]
    public string OtpCode { get; set; }
}