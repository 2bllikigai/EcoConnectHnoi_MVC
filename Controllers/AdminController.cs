using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoConnect_Hanoi.Data; // Đảm bảo đúng namespace của DbContext của bạn
using EcoConnect_Hanoi.Models; // Đảm bảo đúng namespace của Users và các models khác
using System.Security.Claims; // Để lấy User ID
using Microsoft.AspNetCore.Authorization; // Để bảo vệ các action
using Microsoft.AspNetCore.Identity; // Để dùng IPasswordHasher
using Microsoft.Extensions.Logging; // Thêm nếu bạn inject ILogger

// Đổi namespace để không còn nằm trong Area "Admin" nữa
namespace EcoConnect_Hanoi.Controllers // <--- ĐÃ SỬA TÊN NAMESPACE Ở ĐÂY
{
    // Đã loại bỏ [Area("Admin")]
    [Authorize(Roles = "Admin")] // Tất cả các action trong controller này vẫn yêu cầu role Admin
    public class AdminController : Controller
    {
        private readonly EcoConnectHnContext _context;
        private readonly IPasswordHasher<Users> _passwordHasher;
        private readonly ILogger<AdminController> _logger; // Thêm logger nếu cần thiết
        // private readonly IEmailSender _emailSender; // Uncomment nếu bạn muốn dùng email sender

        public AdminController(EcoConnectHnContext context, IPasswordHasher<Users> passwordHasher, ILogger<AdminController> logger /*, IEmailSender emailSender */)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
            // _emailSender = emailSender;
        }

        // Action chính cho Dashboard - Tổng quan hệ thống
        // Nhiệm vụ: Hiển thị các số liệu tổng quan, biểu đồ
        // URL: /Admin/Dashboard (hoặc /Admin nếu là default route cho AdminController)
        public async Task<IActionResult> Dashboard()
        {
            // Các dữ liệu tổng quan như trong hình ảnh dashboard
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalCommunityItems = await _context.CommunityItems.CountAsync();
            // Đếm các tin đăng đang chờ duyệt (giả định có trạng thái ItemStatus.PendingApproval)
            ViewBag.PendingItemsForReview =
                await _context.CommunityItems.CountAsync(i => i.Status == CommunityItems.ItemStatus.Reserved);
            ViewBag.ActiveChallenges = await _context.Challenges.CountAsync(c => c.Status == ChallengeStatus.Active);
            // Tổng số rác tái chế và thử thách hoàn thành (cần logic để lấy từ DB)
            ViewBag.TotalRecycledWasteKg = 3782; // Dữ liệu giả định
            ViewBag.TotalChallengesCompleted = 428; // Dữ liệu giả định

            // Lấy dữ liệu cho biểu đồ "Hoạt động người dùng"
            var userActivityData = await _context.Users
                .GroupBy(u => u.RegistrationDate.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .OrderBy(x => x.Month)
                .ToListAsync();

            // Lấy dữ liệu cho biểu đồ "Phân loại đồ tặng"
            var itemCategoryDistribution = await _context.CommunityItems
                .Include(ci => ci.ItemCategory)
                .GroupBy(ci => ci.ItemCategory.DisplayName)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            var dashboardViewModel = new AdminDashboardViewModel
            {
                TotalUsers = ViewBag.TotalUsers,
                TotalDonatedItems = ViewBag.TotalCommunityItems,
                TotalRecycledWasteKg = ViewBag.TotalRecycledWasteKg,
                TotalChallengesCompleted = ViewBag.TotalChallengesCompleted,
                UserActivityChartData = userActivityData.ToDictionary(x => x.Month, x => x.Count),
                ItemCategoryDistribution = itemCategoryDistribution.ToDictionary(x => x.Category, x => x.Count)
            };

            return View(dashboardViewModel);
        }

        // --- Quản lý Người dùng ---
        // Nhiệm vụ: Quản lý người dùng (thêm, sửa, xóa, thay đổi trạng thái, vai trò, đặt lại mật khẩu)
        // URL: /Admin/ManageUsers
        public async Task<IActionResult> ManageUsers(string searchString)
        {
            var users = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.FirstName.Contains(searchString) ||
                                         u.LastName.Contains(searchString) ||
                                         u.Email.Contains(searchString));
            }
            ViewData["CurrentSearch"] = searchString;
            return View(await users.ToListAsync());
        }

        // GET: /Admin/CreateUser
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(Users user)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem Admin có đang cố gắng tạo một Admin khác không.
                // Logic này tùy thuộc vào quy tắc kinh doanh của bạn.
                // if (user.Role == UserRole.Admin && User.FindFirst(ClaimTypes.Name)?.Value != "your_super_admin_email@example.com")
                // {
                //     ModelState.AddModelError("Role", "Bạn không được phép tạo thêm tài khoản Admin.");
                //     return View(user);
                // }

                user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);
                user.RegistrationDate = DateTime.Now;
                user.LastLoginDate = null;
                user.EcoPoints = 0;
                user.ActivityScore = 0;
                user.IsEmailVerified = false;
                user.AccountStatus = AccountStatus.Pending;

                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Người dùng mới đã được tạo.";
                return RedirectToAction(nameof(ManageUsers)); // <--- ĐÃ BỎ area = "Admin"
            }
            return View(user);
        }

        // GET: /Admin/EditUser/{id}
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Chặn Admin tự chỉnh sửa vai trò của chính mình
            if (user.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value && user.Role == UserRole.Admin)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự chỉnh sửa vai trò của tài khoản Admin của mình tại đây.";
                return RedirectToAction(nameof(ManageUsers)); // <--- ĐÃ BỎ area = "Admin"
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, Users user)
        {
            if (id != user.UserId) return NotFound();

            var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
            if (existingUser == null) return NotFound();

            // Chặn Admin tự chỉnh sửa vai trò của chính mình
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
                    existingUser.Email = user.Email;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.LivingArea = user.LivingArea;
                    existingUser.ProfileImageUrl = user.ProfileImageUrl;
                    existingUser.EnvironmentalInterests = user.EnvironmentalInterests;
                    existingUser.PreferredLanguage = user.PreferredLanguage;
                    existingUser.AccountStatus = user.AccountStatus;
                    existingUser.IsEmailVerified = user.IsEmailVerified;
                    existingUser.Role = user.Role;

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Thông tin người dùng đã được cập nhật.";
                    return RedirectToAction(nameof(ManageUsers)); // <--- ĐÃ BỎ area = "Admin"
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

        // GET: /Admin/DeleteUser/{id}
        [HttpGet]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Admin không được xóa chính mình
            if (user.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(ManageUsers)); // <--- ĐÃ BỎ area = "Admin"
            }

            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Người dùng không tồn tại.";
                return RedirectToAction(nameof(ManageUsers)); // <--- ĐÃ BỎ area = "Admin"
            }

            // Admin không được xóa chính mình
            if (user.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(ManageUsers)); // <--- ĐÃ BỎ area = "Admin"
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Người dùng đã được xóa thành công.";
            return RedirectToAction(nameof(ManageUsers)); // <--- ĐÃ BỎ area = "Admin"
        }

        // POST: /Admin/ChangeAccountStatus
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

        // POST: /Admin/AssignRole
        [HttpPost]
        public async Task<IActionResult> AssignRole(int id, UserRole newRole)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Admin không thể đổi vai trò của chính mình
            if (user.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value &&
                user.Role != newRole)
            {
                 return Json(new { success = false, message = "Bạn không thể tự thay đổi vai trò của mình." });
            }

            user.Role = newRole;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Vai trò của người dùng đã được cập nhật thành {newRole}." });
        }

        // POST: /Admin/ResetUserPassword
        [HttpPost]
        public async Task<IActionResult> ResetUserPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var newTempPassword = Guid.NewGuid().ToString().Substring(0, 8);
            user.PasswordHash = _passwordHasher.HashPassword(user, newTempPassword);
            await _context.SaveChangesAsync();

            try
            {
                // Logic gửi email mật khẩu tạm thời
                // await _emailSender.SendEmailAsync(user.Email, "Mật khẩu tạm thời của bạn cho EcoConnect",
                //                                   $"Mật khẩu tạm thời của bạn là: <strong>{newTempPassword}</strong>. Vui lòng đăng nhập và thay đổi mật khẩu ngay lập tức.");
                return Json(new { success = true, message = "Mật khẩu tạm thời đã được tạo và gửi đến email người dùng (chức năng email chưa được bật)." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email đặt lại mật khẩu cho người dùng {UserId}", user.UserId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi email mật khẩu tạm thời. Vui lòng thử lại sau." });
            }
        }

        // --- Quản lý toàn bộ nội dung thông tin (hướng dẫn, lịch, địa điểm) ---
        // Nhiệm vụ: Quản lý các bài viết, hướng dẫn, lịch thu gom, trung tâm tái chế
        // URL: /Admin/ContentManagement
        public IActionResult ContentManagement()
        {
            // Đây là một trang tổng quan để điều hướng đến các phần quản lý nội dung cụ thể
            return View();
        }

        // Quản lý Hướng dẫn Phân loại & Tái chế (RecyclingCategory)
        // URL: /Admin/ManageRecyclingCategories
        public async Task<IActionResult> ManageRecyclingCategories()
        {
            var categories = await _context.RecyclingCategories.ToListAsync();
            return View(categories);
        }
        // Thêm Create, Edit, Delete cho RecyclingCategory (tương tự như Users)

        // Quản lý Trung tâm Tái chế (RecyclingCenter)
        // URL: /Admin/ManageRecyclingCenters
        public async Task<IActionResult> ManageRecyclingCenters()
        {
            var centers = await _context.RecyclingCenters.ToListAsync();
            return View(centers);
        }
        // Thêm Create, Edit, Delete cho RecyclingCenter

        // Quản lý Lịch Thu Gom (CollectionSchedule)
        // URL: /Admin/ManageCollectionSchedules
        public async Task<IActionResult> ManageCollectionSchedules()
        {
            var schedules = await _context.CollectionSchedules.ToListAsync();
            return View(schedules);
        }
        // Thêm Create, Edit, Delete cho CollectionSchedule

        // --- Quản lý danh mục (loại rác, loại đồ cũ, quận/huyện...) ---
        // Nhiệm vụ: Quản lý các danh mục cho CommunityItems và RecyclingCategories
        // URL: /Admin/CategoryManagement
        public async Task<IActionResult> CategoryManagement()
        {
            ViewBag.ItemCategories = await _context.ItemCategories.ToListAsync();
            ViewBag.RecyclingCategories = await _context.RecyclingCategories.ToListAsync();
            // Thêm các loại danh mục khác nếu có (ví dụ: LivingArea categories)
            return View();
        }
        // Các action CRUD cho từng loại danh mục có thể nằm ở đây hoặc tách ra Controller riêng

        // --- Kiểm duyệt tin đăng cho/tặng/trao đổi ---
        // Nhiệm vụ: Duyệt hoặc từ chối các tin đăng mới
        // URL: /Admin/ReviewCommunityItems
        public async Task<IActionResult> ReviewCommunityItems(string searchString, CommunityItems.ItemStatus? statusFilter)
        {
            var items = _context.CommunityItems
                                .Include(ci => ci.User)
                                .Include(ci => ci.ItemCategory)
                                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                items = items.Where(i => i.Title.Contains(searchString) || i.Description.Contains(searchString));
            }

            if (statusFilter.HasValue)
            {
                items = items.Where(i => i.Status == statusFilter.Value);
            }
            else
            {
                // Mặc định chỉ hiển thị các tin đang chờ duyệt nếu không có filter
                items = items.Where(i => i.Status == CommunityItems.ItemStatus.Reserved);
            }

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentStatusFilter"] = statusFilter;
            return View(await items.OrderByDescending(i => i.CreatedAt).ToListAsync());
        }

        // POST: /Admin/ApproveItem/{id}
        [HttpPost]
        public async Task<IActionResult> ApproveItem(int id)
        {
            var item = await _context.CommunityItems.FindAsync(id);
            if (item == null) return NotFound();

            item.Status = CommunityItems.ItemStatus.Completed;
            _context.Update(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Tin đăng đã được duyệt thành công." });
        }

        // POST: /Admin/RejectItem/{id}
        [HttpPost]
        public async Task<IActionResult> RejectItem(int id, string reason)
        {
            var item = await _context.CommunityItems.FindAsync(id);
            if (item == null) return NotFound();

            item.Status = CommunityItems.ItemStatus.Available;
            // Có thể lưu lý do từ chối vào một trường mới trong CommunityItems hoặc bảng riêng
            _context.Update(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Tin đăng đã bị từ chối." });
        }

        // --- Quản lý hệ thống gamification (thử thách, điểm) ---
        // Nhiệm vụ: Quản lý các thử thách, điều chỉnh điểm EcoPoints
        // URL: /Admin/GamificationManagement
        public IActionResult GamificationManagement()
        {
            // Trang tổng quan cho gamification
            return View();
        }

        // Quản lý Thử thách (Challenge)
        // URL: /Admin/ManageChallenges
        public async Task<IActionResult> ManageChallenges()
        {
            var challenges = await _context.Challenges.OrderByDescending(c => c.StartDate).ToListAsync();
            return View(challenges);
        }
        // Thêm Create, Edit, Delete cho Challenge

        // Điều chỉnh EcoPoints của người dùng
        // URL: /Admin/AdjustEcoPoints
        [HttpGet]
        public async Task<IActionResult> AdjustEcoPoints(int? userId)
        {
            if (userId == null)
            {
                ViewBag.Users = await _context.Users.OrderBy(u => u.FullName).ToListAsync();
                return View(); // Hiển thị danh sách user để chọn
            }
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            return View(user); // Hiển thị form điều chỉnh điểm cho user cụ thể
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustEcoPoints(int userId, int newPoints)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.EcoPoints = newPoints;
            _context.Update(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Điểm EcoPoints của {user.FullName} đã được cập nhật thành {newPoints}.";
            return RedirectToAction(nameof(ManageUsers)); // <--- ĐÃ BỎ area = "Admin"
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }

    // ViewModel cho Dashboard
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalDonatedItems { get; set; }
        public double TotalRecycledWasteKg { get; set; }
        public int TotalChallengesCompleted { get; set; }

        public Dictionary<int, int> UserActivityChartData { get; set; } = new Dictionary<int, int>();
        public Dictionary<string, int> ItemCategoryDistribution { get; set; } = new Dictionary<string, int>();
    }
}