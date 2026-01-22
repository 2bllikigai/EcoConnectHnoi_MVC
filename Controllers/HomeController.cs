using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoConnect_Hanoi.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcoConnect_Hanoi.Controllers
{
    [AllowAnonymous] // Cho phép cả người chưa đăng nhập truy cập
    public class HomeController : Controller
    {
        private readonly EcoConnectHnContext _context;

        public HomeController(EcoConnectHnContext context)
        {
            _context = context;
        }
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            // Mặc định không có thông tin người dùng
            ViewBag.IsLoggedIn = false;
            ViewBag.FullName = "Khách";
            ViewBag.EcoPoints = 0;

            // Nếu đã đăng nhập
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null)
                {
                    int userId = int.Parse(userIdClaim.Value);
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user != null)
                    {
                        ViewBag.IsLoggedIn = true;
                        ViewBag.FullName = user.FirstName + " " + user.LastName;
                        ViewBag.EcoPoints = user.EcoPoints;
                    }
                }
            }

            return View();
        }
    }
}