using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Models; // Thay THLTW_B2 bằng tên project của bạn

namespace THLTW_B2.Areas.Employee.Controllers
{
    [Area("Employee")]
    public class LichSanController : Controller
    {
        private readonly ApplicationDbContext _context; // Thay YourDbContext bằng tên DbContext của bạn

        public LichSanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // HÀM HIỂN THỊ GIAO DIỆN LỊCH
        public IActionResult Index()
        {
            // BẮT BUỘC DÙNG ĐƯỜNG DẪN NÀY VÌ BẠN ĐẶT FILE Ở THƯ MỤC HOME
            return View("~/Areas/Employee/Views/Home/LichSan.cshtml");
        }

        // HÀM TRẢ VỀ DỮ LIỆU JSON CHO FULLCALENDAR
        [HttpGet]
        public async Task<JsonResult> GetCalendarEvents()
        {
            // 1. Lấy Lịch đặt sân
            var bookings = await _context.Bookings
                .Include(b => b.SoccerField)
                .Where(b => b.Status != "Đã hủy")
                .ToListAsync();

            var bookingList = bookings.Select(b => {
                var times = b.TimeSlot.Split('-');
                return new
                {
                    id = "b_" + b.Id,
                    title = (b.SoccerField?.Name ?? "Sân") + " (Đã đặt)",
                    start = b.BookingDate.ToString("yyyy-MM-dd") + "T" + times[0].Trim() + ":00",
                    end = b.BookingDate.ToString("yyyy-MM-dd") + "T" + times[1].Trim() + ":00",
                    color = "#d9534f", // Đỏ đô
                    extendedProps = new { info = "Khách: " + (b.User?.UserName ?? "Khách lẻ") }
                };
            }).ToList();

            // 2. Lấy Kèo tìm đối thủ
            var matches = await _context.MatchRequests.ToListAsync();
            var matchList = matches.Select(m => {
                var times = m.TimeSlot.Split('-');
                return new
                {
                    id = "m_" + m.Id,
                    title = "🔥 Kèo: " + (m.PitchName ?? "Sân"),
                    start = m.MatchDate.ToString("yyyy-MM-dd") + "T" + times[0].Trim() + ":00",
                    end = m.MatchDate.ToString("yyyy-MM-dd") + "T" + times[1].Trim() + ":00",
                    color = "#f0ad4e", // Cam
                    extendedProps = new { info = "Đội: " + m.TeamName }
                };
            }).ToList();

            return Json(bookingList.Concat(matchList).ToList());
        }
    }
}