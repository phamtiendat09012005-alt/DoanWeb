using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;

namespace THLTW_B2.Areas.Admin.Controllers
{

    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class BookingManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Lấy dữ liệu Đặt sân lẻ (Dùng Include để lấy thông tin từ bảng liên kết)
            var bookingsData = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.SoccerField)
                .Select(b => new BookingHistoryViewModel
                {
                    Id = b.Id,
                    Date = b.BookingDate,
                    TimeSlot = b.TimeSlot,
                    PitchName = b.SoccerField.Name,
                    CustomerName = b.User.FullName, // Lấy từ bảng ApplicationUser
                    Type = "Đặt sân lẻ",
                    Status = b.Status,
                    IsMatch = false
                }).ToListAsync();

            // 2. Lấy dữ liệu Kèo ghép
            var matchesData = await _context.MatchRequests
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

            // --- PHÂN LOẠI THEO 3 PHẦN CỦA BẠN ---

            // PHẦN 1: Lịch đã đặt (Gồm đặt sân mới, sân đã hủy, và kèo ghép đã đá xong)
            ViewBag.AllBookings = bookingsData
                .Concat(matchesData.Where(m => m.Status == "Đã có đối thủ" || m.Status.Contains("Hủy")))
                .OrderByDescending(x => x.Date).ToList();

            // PHẦN 2: Lịch đã hoàn thành (Những lịch đặt lẻ và kèo ghép đã đá xong)
            ViewBag.Completed = bookingsData.Where(b => b.Status == "Đã hoàn thành")
                .Concat(matchesData.Where(m => m.Status == "Đã có đối thủ"))
                .OrderByDescending(x => x.Date).ToList();

            // PHẦN 3: Lịch kèo ghép (Toàn bộ các yêu cầu tìm đối thủ)
            ViewBag.MatchRequests = matchesData.OrderByDescending(x => x.Date).ToList();

            return View();
        }
    }
}

