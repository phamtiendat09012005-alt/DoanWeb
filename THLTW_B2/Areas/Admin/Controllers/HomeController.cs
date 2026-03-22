using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

            // 1. Truy vấn DỮ LIỆU THẬT từ bảng Products của bạn
            dashboardData.TotalProducts = _context.Products.Count();
            dashboardData.LowStockProducts = _context.Products.Count(p => p.StockQuantity < 10);

            // 2. Dữ liệu giả lập (Mock data) cho các module của bạn khác chưa làm xong
            dashboardData.TodayRevenue = 2500000;
            dashboardData.NewBookings = 12;
            dashboardData.PendingMatches = 5;

            // Truyền ViewModel ra giao diện
            return View(dashboardData);
        }
    }
}
