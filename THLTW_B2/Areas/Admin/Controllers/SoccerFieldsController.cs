using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace THLTW_B2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SoccerFieldsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public SoccerFieldsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // 1. Danh sách sân
        public async Task<IActionResult> Index()
        {
            return View(await _context.SoccerFields.ToListAsync());
        }

        // 2. GET: Hiển thị form thêm sân
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 3. POST: Xử lý lưu sân + Hình ảnh (ĐÃ GỘP)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SoccerField soccerField, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                // Xử lý Upload ảnh nếu có
                if (file != null)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string fieldPath = Path.Combine(wwwRootPath, @"images/fields");

                    if (!Directory.Exists(fieldPath)) Directory.CreateDirectory(fieldPath);

                    using (var fileStream = new FileStream(Path.Combine(fieldPath, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    soccerField.ImageUrl = @"/images/fields/" + fileName;
                }

                _context.SoccerFields.Add(soccerField);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(soccerField);
        }

        // 4. GET: Hiển thị form sửa sân
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var soccerField = await _context.SoccerFields.FindAsync(id);
            if (soccerField == null) return NotFound();
            return View(soccerField);
        }

        // 5. POST: Xử lý cập nhật thông tin sân
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SoccerField soccerField, IFormFile? file)
        {
            if (id != soccerField.FieldId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý hình ảnh
                    if (file != null)
                    {
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string fieldPath = Path.Combine(wwwRootPath, @"images/fields");

                        // Xóa ảnh cũ nếu có ảnh mới upload lên
                        if (!string.IsNullOrEmpty(soccerField.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, soccerField.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Lưu ảnh mới
                        using (var fileStream = new FileStream(Path.Combine(fieldPath, fileName), FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        soccerField.ImageUrl = @"/images/fields/" + fileName;
                    }

                    _context.Update(soccerField);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SoccerFieldExists(soccerField.FieldId)) return NotFound();
                    else throw;
                }
            }
            // Nếu có lỗi ModelState, nạp lại View kèm dữ liệu cũ
            return View(soccerField);
        }

        private bool SoccerFieldExists(int id)
        {
            return _context.SoccerFields.Any(e => e.FieldId == id);
        }
    }
}