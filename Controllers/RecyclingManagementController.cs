using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models;
using Microsoft.AspNetCore.Authorization;

namespace EcoConnect_Hanoi.Controllers.Admin
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới có quyền truy cập
    [Area("Admin")] // Đặt Controller này vào Area "Admin" để phân biệt đường dẫn
    public class RecyclingManagementController : Controller
    {
        private readonly EcoConnectHnContext _context;

        public RecyclingManagementController(EcoConnectHnContext context)
        {
            _context = context;
        }

        // GET: Admin/RecyclingManagement/Categories
        // Quản lý các loại rác tái chế
        public async Task<IActionResult> Categories()
        {
            return View(await _context.RecyclingCategories.ToListAsync());
        }

        // GET: Admin/RecyclingManagement/CreateCategory
        public IActionResult CreateCategory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory([Bind("Name,Description,RecyclingGuide,ImageUrl")] RecyclingCategory recyclingCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(recyclingCategory);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Loại tái chế mới đã được thêm.";
                return RedirectToAction(nameof(Categories));
            }
            return View(recyclingCategory);
        }

        // Tương tự, thêm các Actions EditCategory, DeleteCategory

        // GET: Admin/RecyclingManagement/Centers
        // Quản lý các trung tâm tái chế
        public async Task<IActionResult> Centers()
        {
            return View(await _context.RecyclingCenters.ToListAsync());
        }

        // Tương tự, thêm các Actions CreateCenter, EditCenter, DeleteCenter
    }
}