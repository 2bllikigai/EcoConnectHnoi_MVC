using System.ComponentModel.DataAnnotations;

namespace EcoConnect_Hanoi.Models.ForgotPassword;

public class ResetPasswordViewModel
{
    [Required]
    public string Email { get; set; }

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; }

    [Compare("NewPassword", ErrorMessage = "Mật khẩu không khớp")]
    public string ConfirmPassword { get; set; }
}