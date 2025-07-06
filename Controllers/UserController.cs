// EcoConnect_Hanoi.Controllers/UserController.cs
using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models; // Users, UserRole, AccountStatus
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using EcoConnect_Hanoi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity; // Thêm namespace này cho SignOutAsync

namespace EcoConnect_Hanoi.Controllers
{
    [Authorize] // Yêu cầu người dùng phải đăng nhập để truy cập bất kỳ action nào trong Controller này
    public class UserController : Controller
    {
        private readonly EcoConnectHnContext _context;
        private readonly IPasswordHasher<Users> _passwordHasher; // Cần inject để hash mật khẩu khi Admin tạo/reset user
        private readonly IEmailSender _emailSender; // Cần inject để gửi email khi Admin reset password

        public UserController(EcoConnectHnContext context, IPasswordHasher<Users> passwordHasher, IEmailSender emailSender)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
        }

        // GET: /User/Index - Trang Dashboard của người dùng đã đăng nhập
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }

        // GET: /User/ManageSystem - Trang quản lý hệ thống (chỉ dành cho Admin)
        [Authorize(Roles = "Admin")] // Chỉ người dùng có vai trò "Admin" mới được phép truy cập
        public async Task<IActionResult> ManageSystem(
            string search = "",
            string filterStatus = "", // account status
            string filterRole = "",   // user role (sẽ chỉ có Admin/User)
            int pageNumber = 1,
            int pageSize = 10)
        {
            IQueryable<Users> users = _context.Users;

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u =>
                    u.Email.Contains(search) ||
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search) ||
                    u.UserId.ToString().Contains(search));
            }

            if (!string.IsNullOrEmpty(filterStatus) && Enum.TryParse(filterStatus, true, out AccountStatus statusEnum))
            {
                users = users.Where(u => u.AccountStatus == statusEnum);
            }

            if (!string.IsNullOrEmpty(filterRole) && Enum.TryParse(filterRole, true, out UserRole roleEnum))
            {
                users = users.Where(u => u.Role == roleEnum);
            }

            var totalUsers = await users.CountAsync();
            var paginatedUsers = await users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentFilterStatus = filterStatus;
            ViewBag.CurrentFilterRole = filterRole;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            ViewBag.AllAccountStatuses = Enum.GetNames(typeof(AccountStatus));
            ViewBag.AllUserRoles = Enum.GetNames(typeof(UserRole)); // Sẽ chỉ là User và Admin

            return View(paginatedUsers);
        }

        // ======================================
        // CÁC HÀNH ĐỘNG QUẢN LÝ NGƯỜI DÙNG (Admin Only)
        // ======================================

        // 4. Controller Actions & Operations - Create User
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(Users user)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem Admin có đang cố gắng tạo một Admin khác không.
                // Nếu người dùng hiện tại là Admin, và họ đang cố gắng tạo một tài khoản Admin khác,
                // bạn có thể cho phép hoặc thêm một lớp bảo mật nếu cần.
                // Đối với một role Admin duy nhất, thường là được phép.
                if (user.Role == UserRole.Admin && User.FindFirst(ClaimTypes.Name)?.Value != "your_super_admin_email@example.com")
                {
                    // Tùy chọn: nếu bạn muốn chỉ 1 email admin duy nhất có thể tạo admin khác
                    // hoặc nếu bạn muốn chặn admin tự tạo thêm admin
                    // ModelState.AddModelError("Role", "Bạn không được phép tạo thêm tài khoản Admin.");
                    // return View(user);
                }


                user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);
                user.RegistrationDate = DateTime.Now;
                user.LastLoginDate = null;
                user.ResetEcopoints();
                user.ResetActivityScore();
                user.IsEmailVerified = false; // Admin có thể chọn True nếu muốn bỏ qua xác minh
                user.AccountStatus = AccountStatus.Pending; // Hoặc Active tùy theo quy trình Admin tạo

                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Người dùng mới đã được tạo.";
                return RedirectToAction(nameof(ManageSystem));
            }
            return View(user);
        }

        // 4. Controller Actions & Operations - Edit User
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Admin không thể chỉnh sửa vai trò của chính mình thông qua đây nếu không có logic đặc biệt
            // Hoặc chặn chỉnh sửa những trường nhạy cảm của chính Admin đang đăng nhập.
            if (user.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            {
                // Có thể chuyển hướng đến trang chỉnh sửa hồ sơ cá nhân thay vì trang quản trị
                // Hoặc chỉ cho phép chỉnh sửa một số thông tin nhất định
            }

            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, Users user)
        {
            if (id != user.UserId) return NotFound();

            // Lấy thông tin người dùng hiện tại từ DB để tránh ghi đè những trường không được chỉnh sửa qua form
            var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
            if (existingUser == null) return NotFound();

            // Admin không thể chỉnh sửa vai trò của chính mình
            if (user.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value &&
                existingUser.Role != user.Role)
            {
                ModelState.AddModelError("Role", "Bạn không thể tự thay đổi vai trò của mình.");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật các trường được phép thay đổi qua form quản trị
                    existingUser.FirstName = user.FirstName;
                    existingUser.LastName = user.LastName;
                    existingUser.Email = user.Email; // Cho phép Admin sửa email
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.LivingArea = user.LivingArea;
                    existingUser.ProfileImageUrl = user.ProfileImageUrl;
                    existingUser.EnvironmentalInterests = user.EnvironmentalInterests;
                    existingUser.PreferredLanguage = user.PreferredLanguage;
                    existingUser.AccountStatus = user.AccountStatus; // Cho phép Admin thay đổi trạng thái
                    existingUser.IsEmailVerified = user.IsEmailVerified; // Cho phép Admin xác minh email thủ công
                    existingUser.Role = user.Role; // Cho phép Admin thay đổi vai trò (sau khi kiểm tra ở trên)
                    // Không cho phép chỉnh sửa trực tiếp EcoPoints và ActivityScore qua form này,
                    // mà thông qua các hành động riêng (nếu có).

                    _context.Update(existingUser); // Cập nhật lại existingUser
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Thông tin người dùng đã được cập nhật.";
                    return RedirectToAction(nameof(ManageSystem));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(user);
        }

        // 4. Controller Actions & Operations - Delete User
        [Authorize(Roles = "Admin")] // Admin được xóa
        [HttpGet]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Admin không được xóa chính mình
            if (user.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(ManageSystem));
            }

            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Người dùng không tồn tại.";
                return RedirectToAction(nameof(ManageSystem));
            }

            // Admin không được xóa chính mình
            if (user.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(ManageSystem));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Người dùng đã được xóa thành công.";
            return RedirectToAction(nameof(ManageSystem));
        }

        // Placeholder cho Suspend/Reactivate User
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ChangeAccountStatus(int id, AccountStatus newStatus)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Admin không thể tự treo/vô hiệu hóa chính mình
            if (user.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value &&
                (newStatus == AccountStatus.Suspended || newStatus == AccountStatus.Deactivated))
            {
                return Json(new { success = false, message = "Bạn không thể tự thay đổi trạng thái tài khoản của mình." });
            }

            user.AccountStatus = newStatus;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Trạng thái tài khoản đã được cập nhật thành {newStatus}." });
        }

        // Placeholder cho Assign Roles
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AssignRole(int id, UserRole newRole)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Admin không thể đổi vai trò của chính mình (từ Admin sang User chẳng hạn)
            if (user.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value &&
                user.Role != newRole)
            {
                 return Json(new { success = false, message = "Bạn không thể tự thay đổi vai trò của mình." });
            }
            // Cũng có thể chặn Admin không cho đổi User khác thành Admin nếu bạn có khái niệm SuperAdmin ẩn.
            // Ví dụ: if (newRole == UserRole.Admin && !User.IsInRole("SuperAdmin_Hidden_Flag")) {...}

            user.Role = newRole;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Vai trò của người dùng đã được cập nhật thành {newRole}." });
        }

        // Placeholder cho Reset Password (tạo mật khẩu tạm thời hoặc gửi link)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ResetUserPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Tạo mật khẩu tạm thời mới
            var newTempPassword = Guid.NewGuid().ToString().Substring(0, 8); // Ví dụ mật khẩu ngẫu nhiên
            user.PasswordHash = _passwordHasher.HashPassword(user, newTempPassword);
            await _context.SaveChangesAsync();

            try
            {
                 // Gửi mật khẩu tạm thời qua email (hoặc chỉ gửi link reset password an toàn hơn)
                await _emailSender.SendEmailAsync(user.Email, "Mật khẩu tạm thời của bạn cho EcoConnect",
                                                   $"Mật khẩu tạm thời của bạn là: <strong>{newTempPassword}</strong>. Vui lòng đăng nhập và thay đổi mật khẩu ngay lập tức.");
                return Json(new { success = true, message = "Mật khẩu tạm thời đã được tạo và gửi đến email người dùng." });
            }
            catch (Exception ex)
            {
                // Log lỗi gửi email
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi email mật khẩu tạm thời. Vui lòng thử lại sau." });
            }
        }


        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}