using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization; // Để lấy UserId

namespace EcoConnect_Hanoi.Controllers
{
    public class CollectionSchedulesController : Controller
    {
        private readonly EcoConnectHnContext _context;

        public CollectionSchedulesController(EcoConnectHnContext context)
        {
            _context = context;
        }

        // GET: /CollectionSchedules/Index
        // Hiển thị lịch thu gom theo khu vực
        public async Task<IActionResult> Index(string area = "")
        {
            IQueryable<CollectionSchedule> schedules = _context.CollectionSchedules;

            if (!string.IsNullOrEmpty(area))
            {
                schedules = schedules.Where(s => s.Area.Contains(area));
                ViewData["CurrentArea"] = area;
            }

            return View(await schedules.OrderBy(s => s.CollectionDay).ToListAsync());
        }

        // GET: /CollectionSchedules/RequestPickup (Tùy chọn: Cho phép người dùng yêu cầu thu gom)
        [HttpGet]
        public IActionResult RequestPickup()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("RequestPickup", "CollectionSchedules") });
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPickup([Bind("PreferredDate,WasteDescription")] UserCollectionRequest request)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                request.UserId = userId;
                request.RequestDate = DateTime.Now;
                request.Status = RequestStatus.Pending; // Sử dụng enum mới cho Status

                _context.Add(request);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Yêu cầu thu gom của bạn đã được gửi thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(request);
        }

        // Tùy chọn: Xem lịch sử yêu cầu thu gom của người dùng
        // GET: /CollectionSchedules/MyRequests
         [Authorize]
         public async Task<IActionResult> MyRequests()
        {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
             var requests = await _context.UserCollectionRequests
                                          .Where(r => r.UserId == userId)
                                         .OrderByDescending(r => r.RequestDate)
                                        .ToListAsync();
            return View(requests);
         }
    }
}