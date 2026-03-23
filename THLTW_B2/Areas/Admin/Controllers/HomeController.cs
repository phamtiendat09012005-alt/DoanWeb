using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // BẮT BUỘC THÊM DÒNG NÀY ĐỂ DÙNG .Include()
using System.Linq;
using THLTW_B2.Models;
using THLTW_B2.DataAccess;

namespace THLTW_B2.Areas.Admin.Controllers
{
    [Area("Admin")] // RẤT QUAN TRỌNG: Báo cho hệ thống biết Controller này thuộc khu vực Admin
    [Authorize(Roles = "Admin")] // BẢO MẬT: Chỉ có tài khoản mang quyền Admin mới được vào đây
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var dashboardData = new DashboardViewModel();
            DateTime now = DateTime.Now; // Lấy thời điểm hiện tại (bao gồm cả giờ phút)
            DateTime today = DateTime.Today; // Lấy ngày hôm nay (00:00:00)

            // 1. QUẢN LÝ KHO (Dữ liệu thật)
            dashboardData.TotalProducts = _context.Products.Count();
            dashboardData.LowStockProducts = _context.Products.Count(p => p.StockQuantity < 10);

            // 2. DOANH THU THỰC TẾ HÔM NAY
            dashboardData.TodayRevenue = _context.Bookings
                .Where(b => b.BookingDate.Date == today && b.Status != "Đã hủy")
                .Sum(b => (decimal?)b.TotalPrice) ?? 0;

            // 3. LƯỢT ĐẶT MỚI TRONG NGÀY
            dashboardData.NewBookings = _context.Bookings
                .Count(b => b.CreatedAt.Date == today);

            // ==================================================================================
            // 4. KÈO ĐANG CHỜ GHÉP (Lấy tất cả kèo từ thời điểm hiện tại trở đi)
            // Giả sử bảng MatchRequest của bạn có trường 'MatchDate' hoặc 'PlayDate' để lưu ngày đá.
            // Nếu bảng dùng chung trường 'BookingDate' thì bạn thay tên trường cho đúng nhé.
            // ==================================================================================
            dashboardData.PendingMatches = _context.MatchRequests
                .Count(m => (m.Status == "Open" || m.Status == "Pending" || m.Status == "Chờ đối")
                         && m.MatchDate >= now);
            // ==================================================================================

            // 5. DANH SÁCH 5 ĐƠN ĐẶT MỚI NHẤT
            dashboardData.RecentBookings = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.SoccerField)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToList();

            return View(dashboardData);
        }
    }
}