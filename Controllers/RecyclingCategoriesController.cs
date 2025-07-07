using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models; // Đảm bảo namespace cho các model mới

namespace EcoConnect_Hanoi.Controllers
{
    // Controller cho người dùng cuối xem thông tin
    public class RecyclingCategoriesController : Controller
    {
        private readonly EcoConnectHnContext _context;

        public RecyclingCategoriesController(EcoConnectHnContext context)
        {
            _context = context;
        }

        // GET: /RecyclingCategories/Index
        // Hiển thị danh sách các loại rác tái chế
        public async Task<IActionResult> Index()
        {
            var categories = await _context.RecyclingCategories.ToListAsync();
            return View(categories);
        }

        // GET: /RecyclingCategories/Details/{id}
        // Hiển thị chi tiết một loại rác tái chế
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.RecyclingCategories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // Tùy chọn: Action để hiển thị danh sách các trung tâm tái chế
        // GET: /RecyclingCategories/RecyclingCenters
        public async Task<IActionResult> RecyclingCenters()
        {
            var centers = await _context.RecyclingCenters.ToListAsync();
            return View(centers);
        }

        // Tùy chọn: Action để tìm kiếm/hiển thị trung tâm gần nhất (cần frontend JavaScript)
        // [HttpGet]
        // public IActionResult FindNearestCenter()
        // {
        //    return View(); // View với bản đồ hoặc form nhập vị trí
        // }
    }
}