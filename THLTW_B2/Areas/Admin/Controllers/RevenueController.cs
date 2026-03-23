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

            // ==========================================
            // 1. TÍNH DOANH THU THUÊ SÂN (Gồm 2 khoản)
            // ==========================================

            // Khoản A: Những đơn khách đã đá xong và thanh toán đủ (100% tiền)
            var completedBookings = await _context.Bookings
                .Where(b => b.Status == "Đã hoàn thành" && b.BookingDate.Month == m && b.BookingDate.Year == y)
                .Include(b => b.User)
                .Include(b => b.SoccerField)
                .ToListAsync();

            decimal fullPaymentRevenue = completedBookings.Sum(b => b.TotalPrice);

            // Khoản B: Những đơn khách mới đặt qua Web (Chỉ mới thu 30% tiền cọc)
            var depositedBookings = await _context.Bookings
                .Where(b => b.Status == "Đã cọc" && b.BookingDate.Month == m && b.BookingDate.Year == y)
                .ToListAsync();

            decimal depositRevenue = depositedBookings.Sum(b => b.DepositAmount);

            // TỔNG CỘNG: Tiền thu đủ + Tiền cọc
            decimal revenueFromBookings = fullPaymentRevenue + depositRevenue;

            // ==========================================
            // 2. TÍNH DOANH THU TỪ TIỀN CỌC KÈO
            // ==========================================
            var depositedMatches = await _context.MatchRequests
                .Where(x => x.IsDeposited == true && x.MatchDate.Month == m && x.MatchDate.Year == y)
                .ToListAsync();

            decimal hostDepositRevenue = depositedMatches.Sum(x => x.TienCoc);
            decimal opponentDepositRevenue = depositedMatches.Where(x => x.IsOpponentDeposited == true).Sum(x => x.TienCoc);

            decimal totalMatchRevenue = hostDepositRevenue + opponentDepositRevenue;

            // ==========================================
            // 3. TÍNH DOANH THU CỬA HÀNG (Bán nước/đồ)
            // ==========================================
            var completedOrders = await _context.Orders
                .Where(o => o.Status == 1 && o.OrderDate.Month == m && o.OrderDate.Year == y)
                .ToListAsync();

            decimal storeRevenue = completedOrders.Sum(o => o.TotalAmount);

            // ==========================================
            // 4. GOM TẤT CẢ LẠI THÀNH TỔNG DOANH THU 
            // ==========================================
            decimal totalRevenue = revenueFromBookings + totalMatchRevenue + storeRevenue;

            // ==========================================
            // 5. TÍNH TỔNG CHI
            // ==========================================
            var expenses = await _context.Expenses
                .Where(e => e.ExpenseDate.Month == m && e.ExpenseDate.Year == y)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            decimal totalExpense = expenses.Sum(e => e.Amount);

            // ==========================================
            // 6. ĐÓNG GÓI DỮ LIỆU GỬI RA GIAO DIỆN
            // ==========================================
            var vm = new RevenueDashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalExpense = totalExpense,
                RecentExpenses = expenses,
                // Bảng lịch sử gần đây: Hiện các đơn đã đá xong (Sếp có thể thêm đơn cọc vào đây sau nếu muốn)
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

            ViewBag.DoanhThuThueSan = revenueFromBookings;
            ViewBag.DoanhThuCapKeo = totalMatchRevenue;
            ViewBag.DoanhThuCuaHang = storeRevenue;

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