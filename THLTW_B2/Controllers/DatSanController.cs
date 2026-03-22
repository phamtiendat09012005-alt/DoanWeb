using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;

namespace THLTW_B2.Controllers
{
    [Authorize] // Bắt buộc khách phải đăng nhập mới được đặt sân
    public class DatSanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DatSanController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // API: Kiểm tra sân trống (Tích hợp cả Booking và MatchRequest)
        // API: Kiểm tra sân trống (Tích hợp cả Booking và MatchRequest)
        // API: Kiểm tra sân trống dành cho trang Đặt Sân Bao Trọn
        [HttpGet]
        public IActionResult GetAvailablePitches(DateTime date, string timeSlot)
        {
            var allPitches = _context.SoccerFields.Where(s => s.IsActive).ToList();

            // 1. Tìm sân đã bị Bao Trọn (Đặt Sân)
            var bookedPitchIds = _context.Bookings
                .Where(b => b.BookingDate.Date == date.Date && b.TimeSlot == timeSlot && b.Status != "Đã hủy")
                .Select(b => b.SoccerFieldId)
                .ToList();

            // 2. Tìm sân ĐÃ CHỐT KÈO (MatchRequest)
            // Dựa đúng vào Model của sếp: Khóa khi OpponentName có dữ liệu HOẶC Status không còn là "Đang chờ đối thủ"
            var matchedPitchNames = _context.MatchRequests
                .Where(m => m.MatchDate.Date == date.Date &&
                            m.TimeSlot == timeSlot &&
                            (m.OpponentName != null || m.OpponentTeamName != null || m.Status != "Đang chờ đối thủ"))
                .Select(m => m.PitchName.Trim())
                .ToList();

            // 3. Trả kết quả
            var result = allPitches.Select(p => new {
                id = p.FieldId,
                name = p.Name,
                price = p.PricePerHour * (decimal)1.5,
                // Khóa sân nếu nằm trong 1 trong 2 danh sách trên
                isBooked = bookedPitchIds.Contains(p.FieldId) || matchedPitchNames.Contains(p.Name.Trim())
            }).ToList();

            return Json(result);
        }

        // API: Lưu Đơn Đặt Sân và xử lý "Cá lớn nuốt cá bé"
        [HttpPost]
        public IActionResult TaoDonDatSan(int soccerFieldId, DateTime date, string timeSlot, decimal price, string note)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pitch = _context.SoccerFields.Find(soccerFieldId);

            if (pitch == null) return Json(new { success = false, message = "Không tìm thấy sân." });

            // 1. Tạo hóa đơn Đặt sân bao trọn
            var booking = new Booking
            {
                UserId = userId,
                SoccerFieldId = soccerFieldId,
                BookingDate = date,
                TimeSlot = timeSlot,
                TotalPrice = price,
                DepositAmount = price * (decimal)0.3,
                Status = "Đã cọc",
                Note = note,
                CreatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);

            // 2. LOGIC CÁ LỚN NUỐT CÁ BÉ: Tìm xem có kèo "Đang chờ đối thủ" nào ở sân này giờ này không?
            var pendingMatches = _context.MatchRequests.Where(m =>
                m.MatchDate.Date == date.Date &&
                m.TimeSlot == timeSlot &&
                m.OpponentName == null &&
                m.PitchName == pitch.Name).ToList();

            // Nếu có -> Hủy kèo đó đi vì Sân đã được khách bao trọn!
            foreach (var match in pendingMatches)
            {
                match.Status = "Đã hủy (Sân có người bao trọn)";
            }

            _context.SaveChanges();

            return Json(new { success = true });
        }


        // API: Đổ dữ liệu lịch kín sân ra Calendar
        [HttpGet]
        public IActionResult GetCalendarEvents()
        {
            var events = new List<object>();

            // 1. Lấy dữ liệu Đặt Sân (Bao trọn)
            var bookings = _context.Bookings.Where(b => b.Status != "Đã hủy").ToList();
            foreach (var b in bookings)
            {
                var pitch = _context.SoccerFields.Find(b.SoccerFieldId);
                string startTime = b.TimeSlot.Split(" - ")[0]; // Cắt lấy "17:30"
                string endTime = b.TimeSlot.Split(" - ")[1];

                events.Add(new
                {
                    title = pitch?.Name + " (Đã đặt)",
                    start = b.BookingDate.ToString("yyyy-MM-dd") + "T" + startTime,
                    end = b.BookingDate.ToString("yyyy-MM-dd") + "T" + endTime,
                    color = "#dc3545", // Màu đỏ cho sân bao trọn
                    textColor = "white"
                });
            }

            // 2. Lấy dữ liệu Cáp Kèo (Đã chốt)
            var matches = _context.MatchRequests.Where(m => m.OpponentName != null).ToList();
            foreach (var m in matches)
            {
                string startTime = m.TimeSlot.Split(" - ")[0];
                string endTime = m.TimeSlot.Split(" - ")[1];

                events.Add(new
                {
                    title = m.PitchName + " (Cáp kèo)",
                    start = m.MatchDate.ToString("yyyy-MM-dd") + "T" + startTime,
                    end = m.MatchDate.ToString("yyyy-MM-dd") + "T" + endTime,
                    color = "#fd7e14", // Màu cam cho sân cáp kèo
                    textColor = "white"
                });
            }

            return Json(events);
        }
    }
}