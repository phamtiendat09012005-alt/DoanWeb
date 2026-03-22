    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using THLTW_B2.Models;
    using THLTW_B2.DataAccess; // Đảm bảo namespace này khớp với ApplicationDbContext của bạn

    namespace THLTW_B2.Areas.Admin.Controllers
    {
        [Area("Admin")]
        public class CategoriesController : Controller
        {
            private readonly ApplicationDbContext _context;

            public CategoriesController(ApplicationDbContext context)
            {
                _context = context;
            }

            // 1. Danh sách danh mục
            public async Task<IActionResult> Index()
            {
                // Lấy kèm danh sách sản phẩm để đếm số lượng
                var categories = await _context.Categories.Include(c => c.Products).ToListAsync();
                return View(categories);
            }

            // 2. Thêm mới danh mục (Giao diện)
            public IActionResult Create() => View();

        // 3. Thêm mới danh mục (Xử lý)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,Name,Description")] Category category)
        {
            // Xóa bỏ lỗi liên quan đến danh sách Products (vì lúc tạo mới thì chưa có sản phẩm nào)
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu bị lỗi, hãy đặt một điểm Breakpoint ở đây để kiểm tra ModelState
            return View(category);
        }

        // 4. Chỉnh sửa (Giao diện)
        public async Task<IActionResult> Edit(int? id)
            {
                if (id == null) return NotFound();
                var category = await _context.Categories.FindAsync(id);
                if (category == null) return NotFound();
                return View(category);
            }

        // 5. Chỉnh sửa (Xử lý)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,Description")] Category category)
        {
            if (id != category.CategoryId) return NotFound();

            // Bắt buộc phải có dòng này vì trong Model Category có ICollection<Product>
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // Đảm bảo cuối file Controller có hàm này:
        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }

        // 6. Xóa (Xử lý - Làm gọn thành 1 bước để xử lý nhanh)
        public async Task<IActionResult> Delete(int? id)
            {
                if (id == null) return NotFound();
                var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(m => m.CategoryId == id);

                if (category.Products.Any())
                {
                    // Nếu còn sản phẩm, không cho xóa để tránh lỗi dữ liệu rác
                    TempData["Error"] = "Không thể xóa vì danh mục này đang chứa sản phẩm!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
        }
    }