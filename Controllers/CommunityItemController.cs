using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models;
using EcoConnect_Hanoi.Models.CommunityItemViewModels; // Import ViewModel
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Cho Authorize attribute
using System.Security.Claims; // Cho User.FindFirstValue
using Microsoft.AspNetCore.Hosting; // Cho IWebHostEnvironment
using System.IO; // Cho Path
using System.Linq; // Cho LINQ methods
using System.Collections.Generic; // Cho List

namespace EcoConnect_Hanoi.Controllers
{
    [Authorize] // Yêu cầu đăng nhập cho toàn bộ Controller
    public class CommunityItemController : Controller
    {
        private readonly EcoConnectHnContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // Inject IWebHostEnvironment

        public CommunityItemController(EcoConnectHnContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Helper load dropdown danh mục
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

        // GET: /CommunityItem
        [AllowAnonymous] // Cho phép xem danh sách mà không cần đăng nhập
        public async Task<IActionResult> Index()
        {
            var items = await _context.CommunityItems
                .Include(i => i.ItemCategory)
                .Include(i => i.User)
                .Include(i => i.Images) // Bao gồm cả hình ảnh
                .ToListAsync();
            return View(items);
        }

        // GET: /CommunityItem/Details/5
        [AllowAnonymous] // Cho phép xem chi tiết mà không cần đăng nhập
        public async Task<IActionResult> Details(int? id) // Dùng int? cho tham số ID
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.CommunityItems
                .Include(i => i.ItemCategory)
                .Include(i => i.User)
                .Include(i => i.Images)
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // GET: /CommunityItem/Create
        public async Task<IActionResult> Create()
        {
            await PopulateCategories();
            return View(new CommunityItemCreateEditViewModel()); // Truyền ViewModel trống
        }

        // POST: /CommunityItem/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommunityItemCreateEditViewModel viewModel)
        {
            // Lấy OwnerUserId từ người dùng hiện tại
            var ownerUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(ownerUserIdString) || !int.TryParse(ownerUserIdString, out int ownerUserId))
            {
                // Nếu không lấy được UserId, có thể người dùng chưa đăng nhập hoặc lỗi.
                // Redirect về trang đăng nhập hoặc hiển thị lỗi.
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để đăng sản phẩm.";
                return RedirectToAction("Login", "Account");
            }

            // Xử lý logic Giveaway/Exchange trước khi kiểm tra ModelState.IsValid
            if (viewModel.Type == CommunityItems.ItemType.Giveaway)
            {
                viewModel.ExchangeWishes = null; // Clear wishes if it's a giveaway
                // Không cần ModelState.Remove nếu ViewModel được thiết kế tốt (ExchangeWishes không Required)
            }

            // Kiểm tra các ràng buộc của ViewModel
            if (ModelState.IsValid)
            {
                // Ánh xạ từ ViewModel sang Entity CommunityItems
                var item = new CommunityItems
                {
                    OwnerUserId = ownerUserId,
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    ItemCategoryId = viewModel.ItemCategoryId,
                    ItemCondition = viewModel.ItemCondition,
                    Type = viewModel.Type,
                    PreferredLocation = viewModel.PreferredLocation,
                    ExchangeWishes = viewModel.ExchangeWishes, // Đã được xử lý ở trên nếu là Giveaway
                    Status = CommunityItems.ItemStatus.Available, // Mặc định là Available
                    CreatedAt = DateTime.UtcNow // Thời gian tạo
                };

                _context.Add(item);
                await _context.SaveChangesAsync(); // Lưu item trước để có ItemId

                // Xử lý upload ảnh
                if (viewModel.ImageFiles != null && viewModel.ImageFiles.Any())
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "communityitems");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    foreach (var file in viewModel.ImageFiles)
                    {
                        if (file.Length > 0)
                        {
                            // Kiểm tra định dạng và kích thước file
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                            
                            // Giới hạn kích thước file (ví dụ: 5MB)
                            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
                            if (file.Length > maxFileSize)
                            {
                                ModelState.AddModelError("ImageFiles", $"Kích thước ảnh '{file.FileName}' vượt quá giới hạn 5MB.");
                                // Không return ngay, để tiếp tục kiểm tra các file khác nếu có nhiều file
                                continue; 
                            }

                            if (!allowedExtensions.Contains(fileExtension))
                            {
                                ModelState.AddModelError("ImageFiles", $"File '{file.FileName}' không phải là ảnh hợp lệ (chỉ chấp nhận JPG, JPEG, PNG, GIF).");
                                continue;
                            }

                            string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            // Tạo ItemImages entity và thêm vào item
                            item.Images.Add(new ItemImages
                            {
                                ImageUrl = "/images/communityitems/" + uniqueFileName, // Lưu đường dẫn tương đối
                                ItemId = item.ItemId // Gán ItemId đã được tạo
                            });
                        }
                    }
                    await _context.SaveChangesAsync(); // Lưu các ảnh đã thêm vào item
                }
                else
                {
                    ModelState.AddModelError("ImageFiles", "Vui lòng tải lên ít nhất một hình ảnh cho sản phẩm.");
                    // Nếu không có ảnh nào, bạn có thể xóa item vừa tạo nếu không muốn nó tồn tại mà không có ảnh
                    _context.CommunityItems.Remove(item);
                    await _context.SaveChangesAsync();
                    await PopulateCategories(viewModel.ItemCategoryId);
                    return View(viewModel);
                }
                
                TempData["SuccessMessage"] = "Sản phẩm đã được đăng thành công.";
                return RedirectToAction(nameof(Index));
            }

            // Nếu ModelState không hợp lệ (ví dụ: validation từ ViewModel), trả về View
            await PopulateCategories(viewModel.ItemCategoryId);
            return View(viewModel);
        }
                [Authorize] // Đảm bảo chỉ người dùng đã đăng nhập mới xem được sản phẩm của họ
                public async Task<IActionResult> UserItems()
                {
                    // Lấy ID của người dùng hiện tại
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
                    if (string.IsNullOrEmpty(currentUserId))
                    {
                        // Nếu không tìm thấy User ID (người dùng chưa đăng nhập hoặc lỗi session),
                        // chuyển hướng đến trang đăng nhập hoặc hiển thị lỗi
                        return RedirectToPage("/Account/Login", new { area = "Identity" });
                    }
        
                    // Lấy các mặt hàng mà OwnerUserId trùng với currentUserId
                    var userItems = await _context.CommunityItems
                                                  .Where(i => i.OwnerUserId.ToString() == currentUserId)
                                                  .Include(i => i.Images)
                                                  .Include(i => i.ItemCategory)
                                                  .Include(i => i.User) // Để hiển thị thông tin người dùng nếu cần
                                                  .ToListAsync();
        
                    ViewData["Title"] = "Sản phẩm của tôi"; // Đặt tiêu đề cho trang
                    return View(userItems); // Tái sử dụng View Index để hiển thị danh sách sản phẩm
                }

        // GET: /CommunityItem/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.CommunityItems
                                     .Include(i => i.Images) // Kèm theo ảnh để hiển thị ảnh cũ
                                     .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền: chỉ chủ sở hữu hoặc admin mới được chỉnh sửa
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (item.OwnerUserId.ToString() != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa sản phẩm này.";
                return Forbid(); // Trả về lỗi 403 Forbidden
            }
            
            // Ánh xạ từ CommunityItems entity sang ViewModel để điền vào form
            var viewModel = new CommunityItemCreateEditViewModel
            {
                ItemId = item.ItemId,
                Title = item.Title,
                Description = item.Description,
                ItemCategoryId = item.ItemCategoryId,
                ItemCondition = item.ItemCondition,
                Type = item.Type,
                PreferredLocation = item.PreferredLocation,
                ExchangeWishes = item.ExchangeWishes,
                ExistingImages = item.Images.ToList() // Truyền ảnh hiện có để hiển thị
            };

            await PopulateCategories(viewModel.ItemCategoryId);
            return View(viewModel);
        }

        // POST: /CommunityItem/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CommunityItemCreateEditViewModel viewModel)
        {
            if (id != viewModel.ItemId)
            {
                return NotFound();
            }

            // Lấy item hiện có từ DB để so sánh và kiểm tra quyền
            var itemToUpdate = await _context.CommunityItems
                                             .Include(i => i.Images)
                                             .FirstOrDefaultAsync(i => i.ItemId == id);

            if (itemToUpdate == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền: chỉ chủ sở hữu hoặc admin mới được chỉnh sửa
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (itemToUpdate.OwnerUserId.ToString() != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa sản phẩm này.";
                return Forbid();
            }

            // Xử lý logic Giveaway/Exchange
            if (viewModel.Type == CommunityItems.ItemType.Giveaway)
            {
                viewModel.ExchangeWishes = null;
            }

            // Cập nhật các thuộc tính từ ViewModel vào entity
            itemToUpdate.Title = viewModel.Title;
            itemToUpdate.Description = viewModel.Description;
            itemToUpdate.ItemCategoryId = viewModel.ItemCategoryId;
            itemToUpdate.ItemCondition = viewModel.ItemCondition;
            itemToUpdate.Type = viewModel.Type;
            itemToUpdate.PreferredLocation = viewModel.PreferredLocation;
            itemToUpdate.ExchangeWishes = viewModel.ExchangeWishes;

            // Kiểm tra ModelState.IsValid sau khi gán các giá trị từ ViewModel
            // Nếu ExchangeWishes bị set null ở trên, ModelState.Remove là cần thiết nếu property đó là [Required] trong entity
            // Nếu bạn đã dùng 'string?' trong entity và không dùng [Required], thì không cần ModelState.Remove nữa.
            // Nếu bạn dùng [Required] trong entity và muốn nó là tùy chọn, ViewModel là cách tốt nhất.
            
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh mới (nếu có)
                if (viewModel.ImageFiles != null && viewModel.ImageFiles.Any())
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "communityitems");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    foreach (var file in viewModel.ImageFiles)
                    {
                        if (file.Length > 0)
                        {
                             // Kiểm tra định dạng và kích thước file (lặp lại logic từ Create)
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
                            if (file.Length > maxFileSize)
                            {
                                ModelState.AddModelError("ImageFiles", $"Kích thước ảnh '{file.FileName}' vượt quá giới hạn 5MB.");
                                continue;
                            }
                            if (!allowedExtensions.Contains(fileExtension))
                            {
                                ModelState.AddModelError("ImageFiles", $"File '{file.FileName}' không phải là ảnh hợp lệ (chỉ chấp nhận JPG, JPEG, PNG, GIF).");
                                continue;
                            }

                            string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            itemToUpdate.Images.Add(new ItemImages
                            {
                                ImageUrl = "/images/communityitems/" + uniqueFileName,
                                ItemId = itemToUpdate.ItemId
                            });
                        }
                    }
                }
                // TODO: Logic xóa ảnh cũ nếu người dùng bỏ chọn ảnh nào đó.
                // Điều này yêu cầu thêm cơ chế vào ViewModel (ví dụ: list các ID ảnh muốn giữ lại)

                try
                {
                    _context.Update(itemToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Thông tin sản phẩm đã được cập nhật thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(itemToUpdate.ItemId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Nếu ModelState không hợp lệ
            await PopulateCategories(viewModel.ItemCategoryId);
            // Đảm bảo ExistingImages được truyền lại để hiển thị ảnh cũ
            viewModel.ExistingImages = itemToUpdate.Images.ToList(); 
            return View(viewModel);
        }

        // GET: /CommunityItem/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.CommunityItems
                .Include(i => i.ItemCategory)
                .Include(i => i.User)
                .Include(i => i.Images) // Kèm theo ảnh
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền: chỉ chủ sở hữu hoặc admin mới được xóa
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (item.OwnerUserId.ToString() != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa sản phẩm này.";
                return Forbid();
            }

            return View(item);
        }

        // POST: /CommunityItem/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.CommunityItems
                                     .Include(i => i.Images) // Bao gồm ảnh để xóa file vật lý
                                     .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền tương tự như Edit
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (item.OwnerUserId.ToString() != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa sản phẩm này.";
                return Forbid();
            }
            
            // Xóa các file ảnh vật lý trước
            foreach (var image in item.Images)
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.CommunityItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Sản phẩm và các ảnh liên quan đã được xóa thành công.";
            return RedirectToAction(nameof(Index));
        }

        private bool ItemExists(int id)
        {
            return _context.CommunityItems.Any(e => e.ItemId == id);
        }
    }
}