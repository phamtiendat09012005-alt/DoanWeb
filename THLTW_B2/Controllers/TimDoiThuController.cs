using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;
using System.Linq; // Thêm để hỗ trợ OrderBy

namespace THLTW_B2.Controllers
{
    public class TimDoiThuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TimDoiThuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TimDoiThu/Index
        public IActionResult Index()
        {
            var matches = _context.MatchRequests
                            .OrderByDescending(m => m.CreatedAt)
                            .ToList();
            return View(matches);
        }

        // POST: TimDoiThu/NhanKeo/5
        [HttpPost]
        public IActionResult NhanKeo(int id)
        {
            // Tìm kèo đấu trong Database dựa vào ID
            var match = _context.MatchRequests.Find(id);

            // Kiểm tra xem kèo có tồn tại và thực sự còn trống hay không
            if (match != null && match.Status == "Đang chờ đối thủ")
            {
                // Cập nhật trạng thái
                match.Status = "Đã có đối thủ";

                // Lưu thay đổi xuống SQL Server
                _context.SaveChanges();
            }

            // Tải lại trang danh sách (gọi lại hàm Index) để giao diện cập nhật ngay lập tức
            return RedirectToAction("Index");
        }
        // API Lấy danh sách Sân thật từ DB và kiểm tra xem sân nào đã bị đặt
        // API Kiểm tra sân rảnh bên trang Cáp Kèo (Đã kết hợp cả 2 bảng)
        // API Kiểm tra sân rảnh DÀNH RIÊNG CHO TRANG CÁP KÈO
        // API Kiểm tra sân rảnh DÀNH RIÊNG CHO TRANG CÁP KÈO
        [HttpGet]
        public IActionResult GetPitchesStatus(DateTime date, string timeSlot)
        {
            var allPitches = _context.SoccerFields.Where(s => s.IsActive).ToList();

            // 1. Tìm sân đã bị Bao Trọn (Đặt Sân)
            var bookedPitchIds = _context.Bookings
                .Where(b => b.BookingDate.Date == date.Date && b.TimeSlot == timeSlot && b.Status != "Đã hủy")
                .Select(b => b.SoccerFieldId)
                .ToList();

            var bookedPitchNames = _context.SoccerFields
                .Where(s => bookedPitchIds.Contains(s.FieldId))
                .Select(s => s.Name.Trim())
                .ToList();

            // 2. Tìm sân ĐÃ CÓ NGƯỜI TẠO KÈO (MatchRequest)
            // Chỉ cần sân đó có trên bảng Cáp Kèo (và chưa bị Hủy) là khóa, không cho người khác tạo thêm!
            var matchedPitchNames = _context.MatchRequests
                .Where(m => m.MatchDate.Date == date.Date &&
                            m.TimeSlot == timeSlot &&
                            !m.Status.Contains("Hủy"))
                .Select(m => m.PitchName.Trim())
                .ToList();

            // 3. Gộp cả 2 danh sách lại
            var allLockedPitches = bookedPitchNames.Concat(matchedPitchNames).ToList();

            // 4. Trả kết quả
            var result = allPitches.Select(p => new {
                name = p.Name,
                price = p.PricePerHour * (decimal)1.5,
                // Bôi xám nếu sân nằm trong "Danh sách đen"
                isBooked = allLockedPitches.Contains(p.Name.Trim())
            }).ToList();

            return Json(result);
        }
    }
}