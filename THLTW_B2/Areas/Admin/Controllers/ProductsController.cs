using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;

namespace THLTW_B2.Areas.Admin.Controllers
{
    [Area("Admin")] // <--- RẤT QUAN TRỌNG: Không có dòng này hệ thống sẽ lỗi 404
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment; 
        }

        // GET: Admin/Products1
        // Thêm tham số searchString vào hàm Index
        public async Task<IActionResult> Index(string searchString)
        {
            // 1. Lưu lại từ khóa tìm kiếm vào ViewData để hiển thị lại trên thanh tìm kiếm sau khi load trang
            ViewData["CurrentFilter"] = searchString;

            // 2. Tạo một câu truy vấn ban đầu (Lấy sản phẩm bao gồm cả thông tin Danh mục)
            // Dùng AsQueryable() để EF Core chưa thực thi ngay, giúp tối ưu hiệu suất
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            // 3. Nếu người dùng có nhập từ khóa, thêm điều kiện lọc (WHERE)
            if (!String.IsNullOrEmpty(searchString))
            {
                // Lọc những sản phẩm có Tên chứa từ khóa
                products = products.Where(p => p.Name.Contains(searchString));
            }

            // 4. Thực thi truy vấn và trả về View
            return View(await products.ToListAsync());
        }

        // GET: Admin/Products1/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Admin/Products1/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        // POST: Admin/Products1/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,Name,Price,StockQuantity,Unit,CategoryId,ImageUpload")] Product product)
        {
            // 1. Lệnh quan trọng: Bảo hệ thống bỏ qua kiểm tra đối tượng Category và ImageUrl
            // vì chúng ta xử lý chúng thủ công, không phải qua Form nhập liệu trực tiếp.
            ModelState.Remove("Category");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                // 2. Xử lý lưu ảnh (Nếu người dùng có chọn ảnh)
                if (product.ImageUpload != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + product.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageUpload.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = "/images/products/" + uniqueFileName;
                }
                else
                {
                    product.ImageUrl = "/images/no-image.png"; // Ảnh mặc định nếu không up
                }

                // 3. Lưu vào Database
                _context.Add(product);
                await _context.SaveChangesAsync();

                // Chuyển hướng về trang danh sách sau khi lưu thành công
                return RedirectToAction(nameof(Index));
            }

            // 4. Nếu code chạy xuống đây nghĩa là có lỗi gì đó (ModelState không hợp lệ)
            // Cần phải nạp lại danh sách Category cho Dropdown, nếu không trang web sẽ bị lỗi trắng (Crash)
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);

            return View(product);
        }

        // GET: Admin/Products1/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Admin/Products1/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Price,StockQuantity,Unit,CategoryId,ImageUrl")] Product product, IFormFile? ImageUpload)
        {
            if (id != product.ProductId) return NotFound();

            // Bỏ qua kiểm tra các trường không nhập từ phím
            ModelState.Remove("Category");
            ModelState.Remove("ImageUpload");

            if (ModelState.IsValid)
            {
                try
                {
                    // XỬ LÝ HÌNH ẢNH
                    if (ImageUpload != null)
                    {
                        // Nếu người dùng chọn file mới -> Tiến hành lưu file
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageUpload.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageUpload.CopyToAsync(fileStream);
                        }

                        // Cập nhật đường dẫn mới vào Model
                        product.ImageUrl = "/images/products/" + uniqueFileName;
                    }
                    // Nếu ImageUpload == null, product.ImageUrl vẫn giữ giá trị từ thẻ hidden (ảnh cũ)

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Admin/Products1/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 1. Tìm sản phẩm cần xóa trong Database
            var product = await _context.Products.FindAsync(id);

            if (product != null)
            {
                // 2. KẾT HỢP DỌN RÁC: Xóa file ảnh vật lý trong thư mục wwwroot
                // Kiểm tra xem sản phẩm có ảnh không, và đảm bảo không xóa nhầm cái ảnh mặc định (no-image.png)
                if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl != "/images/no-image.png")
                {
                    // Cắt bỏ dấu "/" ở đầu chuỗi ImageUrl và ghép với đường dẫn gốc của Server
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'));

                    // Nếu file thực sự tồn tại trên ổ cứng thì xóa nó đi
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // 3. Xóa dữ liệu trong SQL Server
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            // Xóa xong thì quay về trang danh sách
            return RedirectToAction(nameof(Index));
        }
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
