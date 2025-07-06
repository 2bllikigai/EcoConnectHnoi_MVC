using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models; // Users, LoginViewModel, UserRole, AccountStatus
using EcoConnect_Hanoi.Models.ForgotPassword;
using EcoConnect_Hanoi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // Cho IPasswordHasher
using System.Security.Claims; // Cho Claims
using Microsoft.AspNetCore.Authentication; // Cho HttpContext.SignInAsync
using Microsoft.AspNetCore.Authentication.Cookies; // Cho CookieAuthenticationDefaults

namespace EcoConnect_Hanoi.Controllers
{
    public class AccountController : Controller
    {
        private readonly EcoConnectHnContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IPasswordHasher<Users> _passwordHasher;

        public AccountController(EcoConnectHnContext context, IEmailSender emailSender,
            IPasswordHasher<Users> passwordHasher)
        {
            _context = context;
            _emailSender = emailSender;
            _passwordHasher = passwordHasher;
        }

        // ======================================
        // ĐĂNG KÝ TÀI KHOẢN
        // ======================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Users user)
        {
            // Kiểm tra email đã tồn tại trước khi kiểm tra ModelState.IsValid
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký. Vui lòng sử dụng email khác.");
            }

            // Kiểm tra ModelState.IsValid để đảm bảo các ràng buộc (Required, MinLength, Compare...) được đáp ứng
            if (ModelState.IsValid)
            {
                try
                {
                    // Băm mật khẩu người dùng đã nhập (từ user.PasswordHash trong model nhận được từ form)
                    user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);
                    user.RegistrationDate = DateTime.Now;
                    user.Role = UserRole.User; // Người dùng mới mặc định là vai trò 'User'
                    user.AccountStatus = AccountStatus.Pending; // Mặc định là Pending, cần xác minh email
                    user.IsEmailVerified = false;
                    user.LastLoginDate = null;
                    user.ResetEcopoints(); // Đặt điểm Eco và Activity về 0 cho người dùng mới
                    user.ResetActivityScore();

                    _context.Add(user);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Đăng ký tài khoản thành công! Vui lòng kiểm tra email để xác minh tài khoản và sau đó đăng nhập.";
                    return RedirectToAction("Login");
                }
                catch (DbUpdateException ex)
                {
                    // Log lỗi chi tiết nếu cần
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau.");
                }
            }

            // Nếu ModelState không hợp lệ hoặc có lỗi DB, trả về View với dữ liệu đã nhập (trừ mật khẩu) và lỗi
            return View(user);
        }

        // ======================================
        // ĐĂNG NHẬP
        // ======================================

        [HttpGet]
        public IActionResult Login()
        {
            // Lấy thông báo từ TempData để hiển thị trên View
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();
            }

            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"].ToString();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Kiểm tra validation của ViewModel (Email và Password có được nhập không)
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Tìm người dùng theo Email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            // Kiểm tra nếu không tìm thấy người dùng HOẶC mật khẩu không khớp
            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) ==
                PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            // Kiểm tra trạng thái tài khoản
            if (user.AccountStatus == AccountStatus.Pending)
            {
                ModelState.AddModelError("",
                    "Tài khoản của bạn đang chờ xác minh. Vui lòng kiểm tra email để kích hoạt tài khoản.");
                return View(model);
            }

            if (user.AccountStatus == AccountStatus.Suspended)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị tạm khóa. Vui lòng liên hệ hỗ trợ.");
                return View(model);
            }

            if (user.AccountStatus == AccountStatus.Deactivated)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị vô hiệu hóa.");
                return View(model);
            }

            // Cập nhật thời gian đăng nhập cuối cùng
            user.LastLoginDate = DateTime.Now;
            _context.Update(user);
            await _context.SaveChangesAsync();

            // Tạo Claims Identity cho người dùng
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // ID người dùng
                new Claim(ClaimTypes.Name, user.Email), // Tên người dùng (thường là Email)
                new Claim(ClaimTypes.Role, user.Role.ToString()), // Vai trò người dùng (User hoặc Admin)
                new Claim("FullName", user.FullName), // Tên đầy đủ
                new Claim("EcoPoints", user.EcoPoints.ToString()) // Điểm Eco (có thể dùng để hiển thị trên UI)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Cấu hình thuộc tính xác thực (ví dụ: ghi nhớ đăng nhập)
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe, // Ghi nhớ đăng nhập
                ExpiresUtc =
                    DateTimeOffset.UtcNow.AddMinutes(model.RememberMe
                        ? 60 * 24 * 7
                        : 30) // Hết hạn sau 7 ngày nếu ghi nhớ, 30 phút nếu không
            };

            // Đăng nhập người dùng
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Chuyển hướng người dùng dựa trên vai trò
            if (user.Role == UserRole.Admin)
            {
                return RedirectToAction("ManageSystem", "User"); // Chuyển hướng admin đến trang quản trị
            }

            return RedirectToAction("Index", "Home"); // Chuyển hướng người dùng thông thường đến trang chủ
        }

        // ======================================
        // ĐĂNG XUẤT
        // ======================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ======================================
        // QUÊN MẬT KHẨU (FORGOT PASSWORD)
        // ======================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                // Để tránh tiết lộ thông tin người dùng, không báo email không tồn tại.
                // Chỉ báo đã gửi email (hoặc không) để giữ an toàn.
                TempData["SuccessMessage"] =
                    "Nếu địa chỉ email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi.";
                return RedirectToAction("Login"); // Chuyển hướng đến trang đăng nhập thay vì VerifyOtp
            }

            // Tạo mã OTP hoặc token reset password và lưu vào DB/Cache
            var otp = new Random().Next(100000, 999999).ToString();
            // Lưu OTP vào bộ nhớ đệm hoặc một trường tạm thời trong Users model hoặc một bảng riêng
            // Ví dụ: user.ResetPasswordToken = otp; user.TokenExpiration = DateTime.UtcNow.AddMinutes(15); _context.Update(user); await _context.SaveChangesAsync();

            // Gửi OTP qua email
            await _emailSender.SendEmailAsync(user.Email, "Mã OTP đặt lại mật khẩu của bạn",
                $"Mã OTP của bạn là: {otp}. Mã này sẽ hết hạn trong 15 phút.");

            TempData["EmailForOtpVerification"] = user.Email; // Lưu email để VerifyOtp có thể lấy
            TempData["SuccessMessage"] = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.";
            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            if (TempData["EmailForOtpVerification"] == null)
            {
                return RedirectToAction("ForgotPassword"); // Nếu không có email, quay lại ForgotPassword
            }

            ViewBag.Email = TempData["EmailForOtpVerification"]; // Truyền email sang View
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Email = model.Email; // Giữ email khi trả về View
                return View(model);
            }

            // Lấy người dùng dựa trên email và kiểm tra OTP
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            // Ở đây bạn cần so sánh model.Otp với OTP đã lưu trong DB/Cache của user
            // Ví dụ: if (user == null || user.ResetPasswordToken != model.Otp || user.TokenExpiration < DateTime.UtcNow)
            if (user == null /*|| user.ResetPasswordToken != model.Otp || user.TokenExpiration < DateTime.UtcNow*/
               ) // Thay bằng logic kiểm tra OTP thực tế
            {
                ModelState.AddModelError("", "Mã OTP không hợp lệ hoặc đã hết hạn.");
                ViewBag.Email = model.Email;
                return View(model);
            }

            // Nếu OTP hợp lệ, đánh dấu để người dùng có thể đặt lại mật khẩu
            // Ví dụ: user.CanResetPassword = true; _context.Update(user); await _context.SaveChangesAsync();
            TempData["EmailForResetPassword"] = user.Email; // Lưu email để ResetPassword có thể lấy
            TempData["SuccessMessage"] = "Mã OTP đã được xác minh. Bây giờ bạn có thể đặt lại mật khẩu.";
            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["EmailForResetPassword"] == null)
            {
                return RedirectToAction("ForgotPassword"); // Nếu không có quyền reset, quay lại ForgotPassword
            }

            ViewBag.Email = TempData["EmailForResetPassword"]; // Truyền email sang View
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Email = model.Email;
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            // Kiểm tra quyền đặt lại mật khẩu (ví dụ: user.CanResetPassword)
            if (user == null /*|| !user.CanResetPassword*/) // Thay bằng logic kiểm tra quyền reset
            {
                TempData["ErrorMessage"] =
                    "Bạn không có quyền đặt lại mật khẩu. Vui lòng thử lại quy trình quên mật khẩu.";
                return RedirectToAction("ForgotPassword");
            }

            // Băm và cập nhật mật khẩu mới
            user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            // Xóa token/flag reset password
            // user.ResetPasswordToken = null; user.TokenExpiration = null; user.CanResetPassword = false;

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mật khẩu của bạn đã được đặt lại thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // ======================================
        // TRUY CẬP BỊ TỪ CHỐI (Access Denied)
        // ======================================
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}