using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models;
using EcoConnect_Hanoi.Models.CommunityItemViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace EcoConnect_Hanoi.Controllers
{
    [Authorize]
    public class CommunityItemController : Controller
    {
        private readonly EcoConnectHnContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CommunityItemController(EcoConnectHnContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task PopulateCategories(object selected = null)
        {
            var list = await _context.ItemCategories
                .AsNoTracking()
                .Select(c => new SelectListItem {
                    Value = c.ItemCategoryId.ToString(),
                    Text  = c.DisplayName
                })
                .ToListAsync();
            ViewBag.CategoryList = new SelectList(list, "Value", "Text", selected);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            // Sửa: Dùng DbSet CommunityItems nhưng kiểu dữ liệu là CommunityItem
            var items = await _context.CommunityItems
                .Include(i => i.ItemCategory)
                .Include(i => i.User)
                .Include(i => i.Images)
                .ToListAsync();
            return View(items);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.CommunityItems
                .Include(i => i.ItemCategory)
                .Include(i => i.User)
                .Include(i => i.Images)
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item == null) return NotFound();
            return View(item);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateCategories();
            return View(new CommunityItemCreateEditViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommunityItemCreateEditViewModel viewModel)
        {
            var ownerUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(ownerUserIdString) || !int.TryParse(ownerUserIdString, out int ownerUserId))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện.";
                return RedirectToAction("Login", "Account");
            }

            // Sửa: Gọi ItemType.Giveaway trực tiếp (vì đã đưa ra ngoài Class)
            if (viewModel.Type == ItemType.Giveaway)
            {
                viewModel.ExchangeWishes = null;
            }

            if (ModelState.IsValid)
            {
                // Sửa: Khởi tạo model CommunityItem (số ít)
                var item = new CommunityItem
                {
                    OwnerUserId = ownerUserId,
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    ItemCategoryId = viewModel.ItemCategoryId,
                    ItemCondition = viewModel.ItemCondition,
                    Type = viewModel.Type,
                    PreferredLocation = viewModel.PreferredLocation,
                    ExchangeWishes = viewModel.ExchangeWishes,
                    Status = ItemStatus.Available, // Gọi trực tiếp Enum
                    CreatedAt = DateTime.UtcNow
                };

                _context.Add(item);
                await _context.SaveChangesAsync();

                if (viewModel.ImageFiles != null && viewModel.ImageFiles.Any())
                {
                    await HandleFileUploads(viewModel.ImageFiles, item);
                }
                else
                {
                    ModelState.AddModelError("ImageFiles", "Vui lòng tải lên ít nhất một hình ảnh.");
                    _context.CommunityItems.Remove(item);
                    await _context.SaveChangesAsync();
                    await PopulateCategories(viewModel.ItemCategoryId);
                    return View(viewModel);
                }
                
                TempData["SuccessMessage"] = "Đăng sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateCategories(viewModel.ItemCategoryId);
            return View(viewModel);
        }

        // Helper xử lý lưu file ảnh vào ổ D (wwwroot)
        private async Task HandleFileUploads(IEnumerable<IFormFile> files, CommunityItem item)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "communityitems");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            foreach (var file in files)
            {
                if (file.Length > 0 && file.Length <= 5 * 1024 * 1024)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    item.Images.Add(new ItemImages
                    {
                        ImageUrl = "/images/communityitems/" + uniqueFileName,
                        ItemId = item.ItemId
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> UserItems()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return RedirectToAction("Login", "Account");

            var userItems = await _context.CommunityItems
                .Where(i => i.OwnerUserId.ToString() == currentUserId)
                .Include(i => i.Images)
                .Include(i => i.ItemCategory)
                .ToListAsync();

            return View(userItems);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.CommunityItems.Include(i => i.Images).FirstOrDefaultAsync(i => i.ItemId == id);
            if (item == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (item.OwnerUserId.ToString() != currentUserId && !User.IsInRole("Admin")) return Forbid();
            
            var viewModel = new CommunityItemCreateEditViewModel {
                ItemId = item.ItemId,
                Title = item.Title,
                Description = item.Description,
                ItemCategoryId = item.ItemCategoryId,
                ItemCondition = item.ItemCondition,
                Type = item.Type,
                PreferredLocation = item.PreferredLocation,
                ExchangeWishes = item.ExchangeWishes,
                ExistingImages = item.Images.ToList()
            };

            await PopulateCategories(viewModel.ItemCategoryId);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CommunityItemCreateEditViewModel viewModel)
        {
            if (id != viewModel.ItemId) return NotFound();

            var itemToUpdate = await _context.CommunityItems.Include(i => i.Images).FirstOrDefaultAsync(i => i.ItemId == id);
            if (itemToUpdate == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (itemToUpdate.OwnerUserId.ToString() != currentUserId && !User.IsInRole("Admin")) return Forbid();

            if (ModelState.IsValid)
            {
                itemToUpdate.Title = viewModel.Title;
                itemToUpdate.Description = viewModel.Description;
                itemToUpdate.ItemCategoryId = viewModel.ItemCategoryId;
                itemToUpdate.ItemCondition = viewModel.ItemCondition;
                itemToUpdate.Type = viewModel.Type;
                itemToUpdate.PreferredLocation = viewModel.PreferredLocation;
                itemToUpdate.ExchangeWishes = (viewModel.Type == ItemType.Giveaway) ? null : viewModel.ExchangeWishes;

                if (viewModel.ImageFiles != null && viewModel.ImageFiles.Any())
                {
                    await HandleFileUploads(viewModel.ImageFiles, itemToUpdate);
                }

                _context.Update(itemToUpdate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateCategories(viewModel.ItemCategoryId);
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.CommunityItems.Include(i => i.Images).FirstOrDefaultAsync(i => i.ItemId == id);
            if (item == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (item.OwnerUserId.ToString() != currentUserId && !User.IsInRole("Admin")) return Forbid();
            
            foreach (var image in item.Images)
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }

            _context.CommunityItems.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}