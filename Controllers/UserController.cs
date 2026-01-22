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

        public UserController(EcoConnectHnContext context, IPasswordHasher<Users> passwordHasher,
            IEmailSender emailSender)
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
            string filterRole = "", // user role (sẽ chỉ có Admin/User)
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
    }
}