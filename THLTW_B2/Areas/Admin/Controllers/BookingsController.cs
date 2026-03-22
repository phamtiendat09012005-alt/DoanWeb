using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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

        // ============================================================
        // 1. HIỂN THỊ DANH SÁCH CHIA 3 PHẦN
        // ============================================================
        public async Task<IActionResult> Index()
        {
            // A. Lấy dữ liệu từ bảng Đặt Sân Solo
            var soloBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.SoccerField)
                .Select(b => new BookingHistoryViewModel
                {
                    Id = b.Id,
                    Date = b.BookingDate,
                    TimeSlot = b.TimeSlot,
                    PitchName = b.SoccerField.Name,
                    CustomerName = b.User.FullName,
                    Type = "Đặt sân lẻ",
                    Status = b.Status,
                    IsMatch = false
                }).ToListAsync();

            // B. Lấy dữ liệu từ bảng Kèo Ghép
            var matchBookings = await _context.MatchRequests
                .Select(m => new BookingHistoryViewModel
                {
                    Id = m.Id,
                    Date = m.MatchDate,
                    TimeSlot = m.TimeSlot,
                    PitchName = m.PitchName,
                    CustomerName = m.TeamName,
                    OpponentName = m.OpponentName,
                    Type = "Kèo ghép",
                    Status = m.Status,
                    IsMatch = true
                }).ToListAsync();

            // --- PHÂN LOẠI DỮ LIỆU SANG VIEW_BAG ---

            // TAB 1: Lịch đã đặt (Tất cả Solo + Kèo ghép đã có người nhận hoặc đã hủy)
            ViewBag.AllBookings = soloBookings
                .Concat(matchBookings.Where(m => m.Status != "Đang chờ đối thủ"))
                .OrderByDescending(x => x.Date).ToList();

            // TAB 2: Lịch đã hoàn thành (Chỉ những trận đã xác nhận xong)
            ViewBag.Completed = soloBookings.Where(b => b.Status == "Đã hoàn thành")
                .Concat(matchBookings.Where(m => m.Status == "Đã có đối thủ")) // Có thể thêm logic chốt trận sau
                .OrderByDescending(x => x.Date).ToList();

            // TAB 3: Lịch kèo ghép (Danh sách rao tìm đối thủ)
            ViewBag.MatchRequests = matchBookings.OrderByDescending(x => x.Date).ToList();

            return View();
        }

        // 2. GET: Form duyệt/chỉnh sửa trạng thái đơn (Dành cho đặt lẻ)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.SoccerField)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();
            return View(booking);
        }

        // 3. POST: Xử lý cập nhật trạng thái
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string Status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null || id != booking.Id) return NotFound();

            booking.Status = Status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật trạng thái đơn đặt sân thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}