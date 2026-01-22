using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models; // Đã bao gồm các Enum và Model mới
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace EcoConnect_Hanoi.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CommunityItemManagementController : Controller
    {
        private readonly EcoConnectHnContext _context;

        public CommunityItemManagementController(EcoConnectHnContext context)
        {
            _context = context;
        }

        // 1. Index Action: Hiển thị danh sách hàng hóa
        // Đã sửa: Sử dụng trực tiếp ItemStatus? thay vì đường dẫn dài
        public async Task<IActionResult> Index(string searchString, ItemStatus? statusFilter)
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

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentStatusFilter"] = statusFilter;
            
            // Đã sửa: Cast trực tiếp từ ItemStatus
            ViewBag.ItemStatuses = Enum.GetValues(typeof(ItemStatus))
                .Cast<ItemStatus>().ToList();

            // Đã sửa: Lấy số lượng tin chờ duyệt (Status == Reserved)
            ViewBag.PendingItemsForReview = await _context.CommunityItems.CountAsync(ci =>
                ci.Status == ItemStatus.Reserved);

            return View(await items.OrderByDescending(i => i.CreatedAt).ToListAsync());
        }

        // 2. Action cho Modal Details
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var item = await _context.CommunityItems
                .Include(ci => ci.User)
                .Include(ci => ci.ItemCategory)
                .Include(ci => ci.Images)
                .FirstOrDefaultAsync(m => m.ItemId == id);

            if (item == null) return NotFound();

            return PartialView("_DetailsModalPartial", item);
        }

        // 3. Action cho Modal Delete Confirmation
        [HttpGet]
        public async Task<IActionResult> DeleteConfirmationPartial(int id)
        {
            var item = await _context.CommunityItems
                .Include(ci => ci.User)
                .Include(ci => ci.ItemCategory)
                .FirstOrDefaultAsync(m => m.ItemId == id);

            if (item == null) return NotFound();

            return PartialView("_DeleteModalPartial", item);
        }

        // 4. Duyệt tin (Approve)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveItem(int id)
        {
            var item = await _context.CommunityItems.FindAsync(id);
            if (item == null) return NotFound();

            item.Status = ItemStatus.Completed; // Chuyển trạng thái sang Completed
            _context.Update(item);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Tin đăng đã được duyệt thành công.";
            return RedirectToAction(nameof(Review));
        }

        // 5. Từ chối tin (Reject)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectItem(int id, string? reason)
        {
            var item = await _context.CommunityItems.FindAsync(id);
            if (item == null) return NotFound();

            item.Status = ItemStatus.Available; // Trả lại trạng thái Available
            // Lưu lý do từ chối nếu bạn có thuộc tính Note/Reason trong Model
            _context.Update(item);
            await _context.SaveChangesAsync();
            
            TempData["ErrorMessage"] = "Tin đăng đã bị từ chối.";
            return RedirectToAction(nameof(Review));
        }

        // 6. Chỉnh sửa (Edit) - GET
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.CommunityItems.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.ItemCategories = await _context.ItemCategories.ToListAsync();
            ViewBag.Users = await _context.Users.ToListAsync();
            return View(item);
        }

        // 7. Chỉnh sửa (Edit) - POST
        // Đã sửa: Tham số truyền vào là CommunityItem (số ít)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CommunityItem item)
        {
            if (id != item.ItemId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tin đăng đã được cập nhật.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommunityItemExists(item.ItemId)) return NotFound();
                    else throw;
                }
            }
            ViewBag.ItemCategories = await _context.ItemCategories.ToListAsync();
            ViewBag.Users = await _context.Users.ToListAsync();
            return View(item);
        }

        // 8. Xóa vĩnh viễn
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.CommunityItems.FindAsync(id);
            if (item != null)
            {
                _context.CommunityItems.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tin đăng đã được xóa thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 9. Review Action: Xem các tin đang chờ duyệt
        public async Task<IActionResult> Review(string searchString)
        {
            var items = _context.CommunityItems
                .Include(ci => ci.User)
                .Include(ci => ci.ItemCategory)
                .Where(ci => ci.Status == ItemStatus.Reserved) // Lọc các tin chờ duyệt
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                items = items.Where(i => i.Title.Contains(searchString) || i.Description.Contains(searchString));
            }

            ViewData["CurrentSearch"] = searchString;
            return View(await items.OrderByDescending(i => i.CreatedAt).ToListAsync());
        }

        private bool CommunityItemExists(int id)
        {
            return _context.CommunityItems.Any(e => e.ItemId == id);
        }
    }
}