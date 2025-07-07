using System.ComponentModel.DataAnnotations;

namespace EcoConnect_Hanoi.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ tôi?")]
        public bool RememberMe { get; set; }
    }
}