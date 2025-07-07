using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models; // Users, LoginViewModel, UserRole, AccountStatus
using EcoConnect_Hanoi.Models.ForgotPassword;
using EcoConnect_Hanoi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // Cho IPasswordHasher
using System.Security.Claims; // Cho Claims
using System.Text.Encodings.Web; // Thêm namespace này cho UrlEncoder
using Microsoft.AspNetCore.Authentication; // Cho HttpContext.SignInAsync
using Microsoft.AspNetCore.Authentication.Cookies; // Cho CookieAuthenticationDefaults
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization; // Thêm namespace này để dùng Debug.WriteLine

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

                    // Khắc phục lỗi EcoPoints: Đảm bảo EcoPoints có thể được set
                    user.EcoPoints = 0; // Người dùng mới bắt đầu với 0 điểm
                    user.ActivityScore = 0; // Reset ActivityScore nếu có
                    
                    // SỬA ĐỔI CÁCH TẠO TOKEN: Sử dụng Guid.NewGuid().ToString("N") để tránh ký tự đặc biệt trong URL
                    var emailVerificationToken = Guid.NewGuid().ToString("N"); // Định dạng "N" không có dấu gạch ngang
                    user.EmailVerificationToken = emailVerificationToken;
                    user.TokenExpiration = DateTime.UtcNow.AddHours(24); // Token hết hạn sau 24 giờ

                    _context.Add(user);
                    await _context.SaveChangesAsync();

                    // 2. Tạo URL xác minh email
                    // Host của bạn, ví dụ: "localhost:XXXX" hoặc tên miền của bạn khi deploy
                    var callbackUrl = Url.Action(
                        "VerifyEmail", // Tên Action
                        "Account",     // Tên Controller
                        new { userId = user.UserId, token = emailVerificationToken }, // Sử dụng token mới
                        protocol: HttpContext.Request.Scheme);

                    // Debug: In ra URL xác minh để kiểm tra
                    Debug.WriteLine($"Generated verification URL: {callbackUrl}");

                    await _emailSender.SendEmailAsync(
                        user.Email,
                        "Xác minh tài khoản EcoConnect của bạn",
                        $"Vui lòng xác minh tài khoản của bạn bằng cách nhấp vào liên kết này: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Liên kết xác minh</a>");

                    TempData["SuccessMessage"] =
                        "Đăng ký tài khoản thành công! Vui lòng kiểm tra email để xác minh tài khoản và sau đó đăng nhập.";
                    return RedirectToAction("Login");
                }
                catch (DbUpdateException ex)
                {
                    // Log lỗi chi tiết nếu cần
                    Debug.WriteLine($"DbUpdateException during registration: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unexpected error during registration: {ex.Message}");
                    ModelState.AddModelError("", "Đã xảy ra lỗi không xác định khi đăng ký. Vui lòng thử lại.");
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
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole(UserRole.Admin.ToString()))
                {
                    return RedirectToAction("Dashboard", "Admin", new { area = "Admin" }); // Chuyển hướng Admin về Admin Dashboard
                }
                return RedirectToAction("Index", "Home"); // Chuyển hướng User về Home
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
                return RedirectToAction("Dashboard", "Admin"); // Chuyển hướng admin đến trang quản trị
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
                // SỬA LỖI 1: Nếu không tìm thấy người dùng, KHÔNG gửi OTP và KHÔNG chuyển hướng đến VerifyOtp.
                // Chỉ hiển thị thông báo chung và trả về view hiện tại.
                TempData["InfoMessage"] = "Nếu địa chỉ email của bạn tồn tại trong hệ thống, một mã OTP đã được gửi đến bạn.";
                return View(model); // Trả về lại view ForgotPassword để người dùng có thể thử lại
            }

            // --- CHỈ THỰC HIỆN CÁC BƯỚC DƯỚI ĐÂY NẾU user KHÔNG PHẢI LÀ null ---

            // Xóa các OTP cũ chưa sử dụng của email này để tránh xung đột
            var existingOtps = await _context.OtpCodes
                                             .Where(o => o.Email == model.Email && !o.IsUsed && o.ExpireAt > DateTime.Now)
                                             .ToListAsync();
            _context.OtpCodes.RemoveRange(existingOtps);
            await _context.SaveChangesAsync();

            // Tạo mã OTP
            var otpCode = GenerateRandomOtp(); // Hàm tạo OTP (ví dụ: 6 chữ số ngẫu nhiên)
            var expirationTime = DateTime.Now.AddMinutes(15); // OTP hết hạn sau 15 phút

            var newOtp = new OtpCode
            {
                Email = model.Email,
                Code = otpCode,
                CreatedAt = DateTime.Now, // BỔ SUNG: Gán giá trị cho CreatedAt
                ExpireAt = expirationTime,
                IsUsed = false
            };

            _context.OtpCodes.Add(newOtp);
            await _context.SaveChangesAsync();

            // Gửi email chứa mã OTP
            var subject = "Mã OTP đặt lại mật khẩu của bạn";
            var message = $"Mã OTP của bạn là: {otpCode}. Mã này sẽ hết hạn trong 15 phút.";
            await _emailSender.SendEmailAsync(model.Email, subject, message);

            TempData["SuccessMessage"] = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư đến (có thể ở mục spam).";
            return RedirectToAction("VerifyOtp", new { email = model.Email }); // Chuyển hướng đến trang xác minh OTP
        }
private string GenerateRandomOtp()

{

Random rand = new Random();

return rand.Next(100000, 999999).ToString();

}



// ======================================

// VERIFY OTP - BƯỚC 2: XÁC MINH OTP

// ======================================

[HttpGet]

public IActionResult VerifyOtp(string email)

{

if (string.IsNullOrEmpty(email))

{

return RedirectToAction("ForgotPassword");

}

var model = new VerifyOtpViewModel { Email = email };

return View(model);

}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    // Tìm OTP hợp lệ và chưa sử dụng
    var otpRecord = await _context.OtpCodes
        .Where(o => o.Email == model.Email &&
                    o.Code == model.OtpCode &&
                    o.ExpireAt > DateTime.Now && // SỬA LỖI 2: Dùng ExpireAt thay vì CreatedAt
                    !o.IsUsed)
        .OrderByDescending(o => o.ExpireAt) // Lấy OTP mới nhất nếu có nhiều
        .FirstOrDefaultAsync();

    if (otpRecord == null)
    {
        ModelState.AddModelError("", "Mã OTP không hợp lệ hoặc đã hết hạn.");
        return View(model);
    }

    // Đánh dấu OTP đã sử dụng để tránh tái sử dụng
    otpRecord.IsUsed = true;
    _context.Update(otpRecord);
    await _context.SaveChangesAsync();

    // Chuyển hướng đến trang đặt lại mật khẩu với email đã xác minh
    TempData["EmailForReset"] = model.Email; // Lưu email vào TempData
    TempData["SuccessMessage"] = "Mã OTP đã được xác minh thành công. Bây giờ bạn có thể đặt lại mật khẩu.";
    return RedirectToAction("ResetPassword");
}

/// ======================================
        // RESET PASSWORD - BƯỚC 3: ĐẶT LẠI MẬT KHẨU (SAU KHI ĐÃ XÁC MINH OTP)
        // ======================================
        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = TempData["EmailForReset"] as string;
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Vui lòng xác minh OTP trước khi đặt lại mật khẩu.";
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            // Kiểm tra xem email từ TempData có khớp với email trong model gửi lên không
            // Đây là một lớp bảo mật để đảm bảo người dùng không thay đổi email sau khi xác minh OTP
            var verifiedEmail = TempData["EmailForReset"] as string;
            // Quan trọng: Đặt lại TempData để nó không bị mất nếu có lỗi validation và View được trả lại
            // TempData chỉ tồn tại cho 1 request tiếp theo, nên nếu có ModelState.IsValid=false, nó sẽ mất.
            TempData.Keep("EmailForReset"); 


            if (string.IsNullOrEmpty(verifiedEmail) || verifiedEmail != model.Email)
            {
                ModelState.AddModelError("", "Phiên đặt lại mật khẩu không hợp lệ. Vui lòng xác minh OTP lại.");
                // Trả về view với model hiện tại (để giữ các giá trị người dùng đã nhập)
                // Và đảm bảo email được điền đúng
                model.Email = verifiedEmail ?? model.Email; // Ưu tiên email đã xác minh, nếu không thì dùng email từ model
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                // Nếu ModelState không hợp lệ, trả về View với model hiện tại
                // Email đã được đảm bảo ở trên
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Người dùng không tồn tại.";
                // Do đã kiểm tra email ở trên, trường hợp này hiếm khi xảy ra
                // Nhưng vẫn là một kiểm tra an toàn
                return RedirectToAction("ForgotPassword");
            }

            // Băm và cập nhật mật khẩu mới
            user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);

            _context.Update(user);
            await _context.SaveChangesAsync();

            // Xóa email khỏi TempData sau khi sử dụng thành công
            TempData.Remove("EmailForReset");

            TempData["SuccessMessage"] = "Mật khẩu của bạn đã được đặt lại thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }
 [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                TempData["ErrorMessage"] = "Không thể tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                                     // Bao gồm các dữ liệu liên quan để hiển thị trên dashboard
                                     .Include(u => u.CommunityItems) // Đảm bảo bạn có DbSet<CommunityItems> trong DbContext
                                                                    // và User model có Navigation Property cho CommunityItems
                                     // .Include(u => u.WasteCollections) // Nếu có model và DbSet cho lịch sử thu gom
                                     // .Include(u => u.ChallengeParticipations) // Nếu có model và DbSet cho lịch sử thử thách
                                     .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Người dùng không tồn tại.";
                return RedirectToAction("Login");
            }

            // --- TẠO VIEWMODEL ĐỂ TRUYỀN DỮ LIỆU ĐA DẠNG HƠN ---
            // Để thuận tiện, chúng ta sẽ tạo một ViewModel mới.
            // Trước hết, hãy định nghĩa nó ở đâu đó, ví dụ trong thư mục Models/ViewModels/ProfileDashboardViewModel.cs
            // Hoặc đơn giản là định nghĩa trực tiếp dưới đây nếu bạn không muốn tạo file riêng.

            var viewModel = new ProfileDashboardViewModel
            {
                User = user,
                CommunityItems = user.CommunityItems?.OrderByDescending(item => item.CreatedAt).ToList() ?? new List<CommunityItems>(),
                // WasteCollections = user.WasteCollections?.OrderByDescending(wc => wc.CollectionDate).ToList() ?? new List<WasteCollection>(), // Thay bằng tên Model của bạn
                // ChallengeParticipations = user.ChallengeParticipations?.OrderByDescending(cp => cp.DateCompleted).ToList() ?? new List<ChallengeParticipation>() // Thay bằng tên Model của bạn
            };

            // Dữ liệu mock cho Collections và Challenges nếu chưa có model thật
            viewModel.WasteCollections = new List<WasteCollectionMock> // Thay bằng Model thực tế của bạn
            {
                 // new WasteCollectionMock { Id = 1, CollectionDate = DateTime.Now.AddDays(-10), WasteType = "Giấy", WeightKg = 5, EcoPointsEarned = 10 },
                 // new WasteCollectionMock { Id = 2, CollectionDate = DateTime.Now.AddDays(-25), WasteType = "Nhựa", WeightKg = 3, EcoPointsEarned = 6 }
            };

            viewModel.ChallengeParticipations = new List<ChallengeParticipationMock> // Thay bằng Model thực tế của bạn
            {
                 // new ChallengeParticipationMock { Id = 1, ChallengeName = "Giảm thiểu nhựa dùng 1 lần", DateCompleted = DateTime.Now.AddMonths(-1), PointsEarned = 50 },
                 // new ChallengeParticipationMock { Id = 2, ChallengeName = "Tái chế đúng cách", DateCompleted = DateTime.Now.AddMonths(-2), PointsEarned = 30 }
            };

            return View(viewModel);
        }

        // ======================================
        // CÁC MODEL MOCK (TẠM THỜI) CHO DEMO
        // ======================================
        // Bạn cần thay thế chúng bằng các Model thực tế của bạn
        // hoặc xóa nếu bạn đã có các model tương ứng trong project
        public class WasteCollectionMock
        {
            public int Id { get; set; }
            public DateTime CollectionDate { get; set; }
            public string WasteType { get; set; }
            public double WeightKg { get; set; }
            public int EcoPointsEarned { get; set; }
        }

        public class ChallengeParticipationMock
        {
            public int Id { get; set; }
            public string ChallengeName { get; set; }
            public DateTime DateCompleted { get; set; }
            public int PointsEarned { get; set; }
        }

        // ======================================
        // VIEWMODEL CHO DASHBOARD CỦA PROFILE
        // ======================================
        // Định nghĩa ViewModel này bên trong namespace của bạn, hoặc tốt hơn là tạo file riêng
        // ví dụ: EcoConnect_Hanoi.Models/ViewModels/ProfileDashboardViewModel.cs
        public class ProfileDashboardViewModel
        {
            public Users User { get; set; }
            public List<CommunityItems> CommunityItems { get; set; }
            public List<WasteCollectionMock> WasteCollections { get; set; } // Thay bằng Model thực tế của bạn
            public List<ChallengeParticipationMock> ChallengeParticipations { get; set; } // Thay bằng Model thực tế của bạn
        }

        // TRUY CẬP BỊ TỪ CHỐI (Access Denied)
        // ======================================
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}