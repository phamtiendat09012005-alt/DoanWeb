using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
// Thay thế bằng namespace chuẩn của dự án bạn
using THLTW_B2.DataAccess;
using THLTW_B2.Models;

namespace THLTW_B2.Areas.Employee.Controllers
{
    [Area("Employee")]
    public class DatSanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DatSanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Action hiển thị màn hình Quản lý lịch đặt
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách Booking kèm theo thông tin Sân (SoccerField)
            // Sắp xếp lịch mới nhất hoặc lịch sắp diễn ra lên đầu
            var bookings = await _context.Bookings
        .Include(b => b.SoccerField) // Lấy thông tin sân
        .Include(b => b.User)        // Lấy thông tin người đặt (QUAN TRỌNG)
        .OrderByDescending(b => b.CreatedAt) // Sắp xếp theo ngày tạo
        .ToListAsync();

            return View(bookings);
        }

        // Action dùng để duyệt đơn (cập nhật trạng thái)
        [HttpPost]
        public async Task<IActionResult> XacNhanDon(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                booking.Status = "Đã xác nhận"; // Hoặc trạng thái tương ứng trong hệ thống của bạn
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã duyệt đơn thành công!" });
            }
            return Json(new { success = false, message = "Không tìm thấy đơn." });
        }
    }
}