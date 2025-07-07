using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace EcoConnect_Hanoi.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CommunityItemManagementController : Controller
    {
        private readonly EcoConnect_Hanoi.Data.EcoConnectHnContext _context;

        public CommunityItemManagementController(EcoConnectHnContext context)
        {
            _context = context;
        }

        // Index Action (giữ nguyên)
        public async Task<IActionResult> Index(string searchString,
            EcoConnect_Hanoi.Models.CommunityItems.ItemStatus? statusFilter)
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
            ViewBag.ItemStatuses = Enum.GetValues(typeof(EcoConnect_Hanoi.Models.CommunityItems.ItemStatus))
                .Cast<EcoConnect_Hanoi.Models.CommunityItems.ItemStatus>().ToList();

            // Lấy số lượng tin chờ duyệt cho nút "Tin chờ duyệt"
            ViewBag.PendingItemsForReview = await _context.CommunityItems.CountAsync(ci =>
                ci.Status == EcoConnect_Hanoi.Models.CommunityItems.ItemStatus.Reserved);

            return View(await items.OrderByDescending(i => i.CreatedAt).ToListAsync());
        }

        // Action mới cho Modal Details
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var item = await _context.CommunityItems
                .Include(ci => ci.User)
                .Include(ci => ci.ItemCategory)
                .Include(ci => ci.Images)
                .FirstOrDefaultAsync(m => m.ItemId == id);
            if (item == null)
            {
                return NotFound();
            }

            return PartialView("_DetailsModalPartial", item); // Trả về Partial View cho Modal
        }

        // Action mới cho Modal Delete Confirmation
        [HttpGet]
        public async Task<IActionResult> DeleteConfirmationPartial(int id)
        {
            var item = await _context.CommunityItems
                .Include(ci => ci.User)
                .Include(ci => ci.ItemCategory)
                .FirstOrDefaultAsync(m => m.ItemId == id);
            if (item == null)
            {
                return NotFound();
            }

            return PartialView("_DeleteModalPartial", item); // Trả về Partial View cho Modal
        }

        // Giữ nguyên ApproveItem và RejectItem cho phần Review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveItem(int id)
        {
            var item = await _context.CommunityItems.FindAsync(id);
            if (item == null) return NotFound();

            item.Status = EcoConnect_Hanoi.Models.CommunityItems.ItemStatus.Completed; // Sử dụng đúng enum
            _context.Update(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tin đăng đã được duyệt thành công.";
            return RedirectToAction(nameof(Review)); // Hoặc Index tùy thuộc vào luồng bạn muốn
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectItem(int id, string? reason)
        {
            var item = await _context.CommunityItems.FindAsync(id);
            if (item == null) return NotFound();

            item.Status = EcoConnect_Hanoi.Models.CommunityItems.ItemStatus.Available; // Sử dụng đúng enum
            // ... (logic lưu lý do từ chối)
            _context.Update(item);
            await _context.SaveChangesAsync();
            TempData["ErrorMessage"] = "Tin đăng đã bị từ chối.";
            return RedirectToAction(nameof(Review)); // Hoặc Index
        }

        // Giữ nguyên Edit và DeleteConfirmed
        // GET: Admin/CommunityItemManagement/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.CommunityItems.FindAsync(id);
            if (item == null) return NotFound();
            ViewBag.ItemCategories = await _context.ItemCategories.ToListAsync();
            ViewBag.Users = await _context.Users.ToListAsync();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EcoConnect_Hanoi.Models.CommunityItems item)
        {
            if (id != item.ItemId) return NotFound();

            var existingItem = await _context.CommunityItems.AsNoTracking().FirstOrDefaultAsync(i => i.ItemId == id);
            if (existingItem == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    existingItem.Title = item.Title;
                    existingItem.Description = item.Description;
                    existingItem.ItemCategoryId = item.ItemCategoryId;
                    existingItem.OwnerUserId = item.OwnerUserId;
                    existingItem.Type = item.Type;
                    existingItem.ItemCondition = item.ItemCondition;
                    existingItem.PreferredLocation = item.PreferredLocation;
                    existingItem.CreatedAt = item.CreatedAt;
                    existingItem.Status = item.Status;

                    _context.Update(existingItem);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tin đăng đã được cập nhật.";
                    return RedirectToAction(nameof(Index), new { area = "Admin" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommunityItemExists(item.ItemId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.ItemCategories = await _context.ItemCategories.ToListAsync();
            ViewBag.Users = await _context.Users.ToListAsync();
            return View(item);
        }

        // POST: Admin/CommunityItemManagement/DeleteConfirmed/{id}
        [HttpPost, ActionName("Delete")] // Tên Action vẫn là "Delete" nhưng dùng HttpPost
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

        private bool CommunityItemExists(int id)
        {
            return _context.CommunityItems.Any(e => e.ItemId == id);
        }

        // Action Review (giữ nguyên hoặc điều chỉnh tùy ý)
        public async Task<IActionResult> Review(string searchString)
        {
            var items = _context.CommunityItems
                .Include(ci => ci.User)
                .Include(ci => ci.ItemCategory)
                .Where(ci =>
                    ci.Status == EcoConnect_Hanoi.Models.CommunityItems.ItemStatus.Reserved) // Sử dụng đúng enum
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                items = items.Where(i => i.Title.Contains(searchString) || i.Description.Contains(searchString));
            }

            ViewData["CurrentSearch"] = searchString;
            return View(await items.OrderByDescending(i => i.CreatedAt).ToListAsync());
        }
    }
}