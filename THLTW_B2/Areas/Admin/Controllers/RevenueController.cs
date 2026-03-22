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

            // 1. TÍNH DOANH THU THUÊ SÂN (Từ các đơn Đã hoàn thành trong tháng)
            var completedBookings = await _context.Bookings
                .Where(b => b.Status == "Đã hoàn thành" && b.BookingDate.Month == m && b.BookingDate.Year == y)
                .Include(b => b.User)
                .Include(b => b.SoccerField)
                .ToListAsync();

            decimal revenueFromBookings = completedBookings.Sum(b => b.TotalPrice);

            // ==========================================
            // ĐÃ SỬA: 2. TÍNH DOANH THU TỪ TIỀN CỌC KÈO
            // ==========================================
            // Lấy tất cả các kèo đã được cọc (IsDeposited == true) trong tháng
            var depositedMatches = await _context.MatchRequests
                .Where(x => x.IsDeposited == true && x.MatchDate.Month == m && x.MatchDate.Year == y)
                .ToListAsync();

            // Lấy TỔNG TIỀN CỌC của người tạo kèo (Host)
            decimal hostDepositRevenue = depositedMatches.Sum(x => x.TienCoc);

            // Lấy TỔNG TIỀN CỌC của người nhận kèo (Opponent) - Giả sử mặc định là 100k như giao diện QR
            // (Nếu sau này bạn có cột TienCocNhanKeo thì thay vào đây)
            var opponentDepositedMatchesCount = depositedMatches.Count(x => x.IsOpponentDeposited == true);
            decimal opponentDepositRevenue = opponentDepositedMatchesCount * 100000;

            // TỔNG CỘNG DOANH THU CÁP KÈO
            decimal totalMatchRevenue = hostDepositRevenue + opponentDepositRevenue;

            // TỔNG DOANH THU = Thuê sân + Cọc kèo
            decimal totalRevenue = revenueFromBookings + totalMatchRevenue;

            // 3. TÍNH TỔNG CHI
            var expenses = await _context.Expenses
                .Where(e => e.ExpenseDate.Month == m && e.ExpenseDate.Year == y)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            decimal totalExpense = expenses.Sum(e => e.Amount);

            // 4. ĐÓNG GÓI DỮ LIỆU
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

            // Gửi riêng dữ liệu cọc kèo ra View để hiển thị nếu cần
            ViewBag.DoanhThuThueSan = revenueFromBookings;
            ViewBag.DoanhThuCapKeo = totalMatchRevenue;

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