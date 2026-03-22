using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;
using System.Threading.Tasks;
using System.Linq;

namespace THLTW_B2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Hiển thị danh sách Đặt sân bao trọn
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)        // Nối bảng lấy User
                .Include(b => b.SoccerField) // Nối bảng lấy Sân
                .OrderByDescending(b => b.CreatedAt) // Mới nhất lên đầu
                .ToListAsync();

            return View(bookings);
        }

        // 2. GET: Form duyệt/chỉnh sửa trạng thái đơn
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.SoccerField)
                .FirstOrDefaultAsync(m => m.Id == id); // Sửa thành m.Id theo đúng Model của bạn

            if (booking == null) return NotFound();
            return View(booking);
        }

        // 3. POST: Xử lý cập nhật trạng thái
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string Status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null || id != booking.Id) return NotFound(); // Sửa thành booking.Id

            // Cập nhật trạng thái
            booking.Status = Status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật trạng thái đơn đặt sân thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}