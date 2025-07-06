using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoConnect_Hanoi.Controllers
{
    public class CommunityItemController : Controller
    {
        private readonly EcoConnectHnContext _context;
        public CommunityItemController(EcoConnectHnContext context)
            => _context = context;

        // Helper load dropdown danh mục
        private void PopulateCategories(object selected = null)
        {
            var list = _context.ItemCategories
                .AsNoTracking()
                .Select(c => new SelectListItem {
                    Value = c.ItemCategoryId.ToString(),
                    Text  = c.DisplayName
                })
                .ToList();
            ViewBag.CategoryList = new SelectList(list, "Value", "Text", selected);
        }

        // GET: /CommunityItem
        public IActionResult Index()
        {
            var items = _context.CommunityItems
                .Include(i => i.ItemCategory)
                .Include(i => i.User)
                .ToList();
            return View(items);
        }

        // GET: /CommunityItem/Details/5
        public IActionResult Details(int id)
        {
            var item = _context.CommunityItems
                .Include(i => i.ItemCategory)
                .Include(i => i.User)
                .Include(i => i.Images)
                .FirstOrDefault(i => i.ItemId == id);
            if (item == null) return NotFound();
            return View(item);
        }

        // GET: /CommunityItem/Create
        public IActionResult Create()
        {
            PopulateCategories();
            return View();
        }

        // POST: /CommunityItem/Create
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(CommunityItems item /*, IFormFileCollection ImageFiles, ... */)
        {
            
            // nếu user chọn Giveaway thì clear ExchangeWishes
            if (item.Type == CommunityItems.ItemType.Giveaway)
            {
                item.ExchangeWishes = null;
                ModelState.Remove(nameof(item.ExchangeWishes));
            }
            if (ModelState.IsValid)
            {
                item.Status    = CommunityItems.ItemStatus.Available;
                item.CreatedAt = DateTime.UtcNow;
                _context.CommunityItems.Add(item);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            // POST lỗi, phải reload danh mục trước khi trả View
            PopulateCategories(item.ItemCategoryId);
            return View(item);
        }

        // GET: /CommunityItem/Edit/5
        public IActionResult Edit(int id)
        {
            var item = _context.CommunityItems.Find(id);
            if (item == null) return NotFound();

            PopulateCategories(item.ItemCategoryId);
            return View(item);
        }

        // POST: /CommunityItem/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(int id, CommunityItems item)
        {
            
            if (item.Type == CommunityItems.ItemType.Giveaway)
            {
                item.ExchangeWishes = null;
                ModelState.Remove(nameof(item.ExchangeWishes));
            }
            if (id != item.ItemId)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.CommunityItems.Update(item);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            // POST lỗi, reload danh mục
            PopulateCategories(item.ItemCategoryId);
            return View(item);
        }

        // GET: /CommunityItem/Delete/5
        public IActionResult Delete(int id)
        {
            var item = _context.CommunityItems
                .Include(i => i.ItemCategory)
                .FirstOrDefault(i => i.ItemId == id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: /CommunityItem/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var item = _context.CommunityItems.Find(id);
            if (item != null)
            {
                _context.CommunityItems.Remove(item);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
