using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace THLTW_B2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RevenueController : Controller
    {
        private readonly ApplicationDbContext _context;
        public RevenueController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(int? month, int? year)
        {
            int m = month ?? DateTime.Now.Month;
            int y = year ?? DateTime.Now.Year;

            // 1. TÍNH TỔNG THU (Từ các đơn Đã hoàn thành trong tháng)
            var completedBookings = await _context.Bookings
                .Where(b => b.Status == "Đã hoàn thành" && b.BookingDate.Month == m && b.BookingDate.Year == y)
                .Include(b => b.User)
                .Include(b => b.SoccerField)
                .ToListAsync();

            var completedMatches = await _context.MatchRequests
                .Where(x => x.Status == "Đã hoàn thành" && x.MatchDate.Month == m && x.MatchDate.Year == y)
                .ToListAsync();

            // ---------------------------------------------------------
            // [MỚI THÊM NÈ SẾP]: Lấy doanh thu từ Cửa hàng (Hóa đơn đã thanh toán)
            // ---------------------------------------------------------
            var completedOrders = await _context.Orders
                .Where(o => o.Status == 1 && o.OrderDate.Month == m && o.OrderDate.Year == y)
                .ToListAsync();

            // CỘNG GỘP 3 NGUỒN TIỀN LẠI: Đặt sân + Ghép kèo + Bán nước/đồ
            decimal totalRevenue = completedBookings.Sum(b => b.TotalPrice)
                                 + completedMatches.Sum(x => 100000) // Giả sử kèo ghép thu phí 100k
                                 + completedOrders.Sum(o => o.TotalAmount); // <--- TIỀN BÁN NƯỚC CHẢY VÀO ĐÂY!
                                                                            // ---------------------------------------------------------

            // 2. TÍNH TỔNG CHI
            var expenses = await _context.Expenses
                .Where(e => e.ExpenseDate.Month == m && e.ExpenseDate.Year == y)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            decimal totalExpense = expenses.Sum(e => e.Amount);

            // 3. ĐÓNG GÓI DỮ LIỆU
            var vm = new RevenueDashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalExpense = totalExpense,
                RecentExpenses = expenses,
                RecentRevenues = completedBookings.Select(b => new BookingHistoryViewModel
                {
                    Id = b.Id,
                    Date = b.BookingDate,
                    CustomerName = b.User?.FullName,
                    PitchName = b.SoccerField?.Name,
                    TotalPrice = b.TotalPrice
                }).ToList()
            };

            ViewBag.SelectedMonth = m;
            ViewBag.SelectedYear = y;

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AddExpense(Expense model)
        {
            if (ModelState.IsValid)
            {
                model.ExpenseDate = DateTime.Now;
                _context.Expenses.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã ghi nhận khoản chi thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}