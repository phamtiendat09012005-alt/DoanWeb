using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;

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

        public async Task<IActionResult> Index()
        {
            // 1. Dữ liệu Đặt sân lẻ (Solo)
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

            // 2. Dữ liệu Kèo ghép (Match)
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

            var allData = soloBookings.Concat(matchBookings).ToList();

            // 3. Phân loại theo đúng nghiệp vụ Admin
            // Tab 1: Đang hoạt động (Đã cọc, Đã có đối thủ) & Đã hủy
            ViewBag.ActiveBookings = allData
                .Where(x => x.Status == "Đã cọc" || x.Status == "Đã có đối thủ" || x.Status == "Đã hủy")
                .OrderByDescending(x => x.Date).ToList();

            // Tab 2: Lịch sử đã thu tiền
            ViewBag.CompletedBookings = allData
                .Where(x => x.Status == "Đã hoàn thành")
                .OrderByDescending(x => x.Date).ToList();

            // Tab 3: Bảng theo dõi Kèo đang chờ
            ViewBag.PendingMatches = matchBookings
                .Where(m => m.Status == "Đang chờ đối thủ")
                .OrderByDescending(x => x.Date).ToList();

            return View();
        }

        // Xử lý nút bấm: Hoàn thành / Hủy đơn (Dùng AJAX)
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, bool isMatch, string newStatus)
        {
            try
            {
                if (isMatch)
                {
                    var match = await _context.MatchRequests.FindAsync(id);
                    if (match == null) return Json(new { success = false, message = "Không tìm thấy kèo." });
                    match.Status = newStatus;
                }
                else
                {
                    var booking = await _context.Bookings.FindAsync(id);
                    if (booking == null) return Json(new { success = false, message = "Không tìm thấy đơn đặt." });
                    booking.Status = newStatus;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // ===================================================================
        // HÀM MỚI BỔ SUNG: XEM CHI TIẾT HÓA ĐƠN & BIÊN BẢN TRẬN ĐẤU
        // ===================================================================
        public async Task<IActionResult> Details(int id, bool isMatch)
        {
            if (isMatch)
            {
                // Nếu là kèo ghép -> Gọi file MatchDetails.cshtml
                var match = await _context.MatchRequests.FirstOrDefaultAsync(m => m.Id == id);
                if (match == null) return NotFound();
                return View("MatchDetails", match);
            }
            else
            {
                // Nếu là đặt sân lẻ -> Gọi file Details.cshtml mặc định
                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.SoccerField)
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (booking == null) return NotFound();
                return View(booking);
            }
        }
    }
}