using System.ComponentModel.DataAnnotations;

namespace EcoConnect_Hanoi.Models.ForgotPassword;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}