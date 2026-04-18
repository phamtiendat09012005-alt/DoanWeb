using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            // 1. TRUY VẤN DỮ LIỆU BOOKING (Đã hoàn thành & Đã cọc)
            // ==========================================

            // Lấy đơn hoàn thành (Thu 100% tiền)
            var completedBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.SoccerField)
                .Where(b => b.Status == "Đã hoàn thành" && b.BookingDate.Month == m && b.BookingDate.Year == y)
                .ToListAsync();

            // Lấy đơn đã cọc (Chỉ thu tiền cọc)
            var depositedBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.SoccerField)
                .Where(b => b.Status == "Đã cọc" && b.BookingDate.Month == m && b.BookingDate.Year == y)
                .ToListAsync();

            // Tính toán doanh thu
            decimal fullPaymentRevenue = completedBookings.Sum(b => b.TotalPrice);
            decimal depositRevenue = depositedBookings.Sum(b => b.DepositAmount);
            decimal revenueFromBookings = fullPaymentRevenue + depositRevenue;

            // ==========================================
            // 2. DOANH THU CỌC KÈO & CỬA HÀNG (Giữ nguyên logic của bạn)
            // ==========================================
            var depositedMatches = await _context.MatchRequests
                .Where(x => x.IsDeposited == true && x.MatchDate.Month == m && x.MatchDate.Year == y)
                .ToListAsync();
            decimal totalMatchRevenue = depositedMatches.Sum(x => x.TienCoc) + depositedMatches.Where(x => x.IsOpponentDeposited == true).Sum(x => x.TienCoc);

            var completedOrders = await _context.Orders
                .Where(o => o.Status == 1 && o.OrderDate.Month == m && o.OrderDate.Year == y)
                .ToListAsync();
            decimal storeRevenue = completedOrders.Sum(o => o.TotalAmount);

            decimal totalRevenue = revenueFromBookings + totalMatchRevenue + storeRevenue;

            // ==========================================
            // 3. TÍNH TỔNG CHI
            // ==========================================
            var expenses = await _context.Expenses
                .Where(e => e.ExpenseDate.Month == m && e.ExpenseDate.Year == y)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();
            decimal totalExpense = expenses.Sum(e => e.Amount);

            // ==========================================
            // 4. GỘP DANH SÁCH LỊCH SỬ THU (QUAN TRỌNG)
            // ==========================================
            var historyList = new List<BookingHistoryViewModel>();

            // Thêm các đơn hoàn thành vào lịch sử
            historyList.AddRange(completedBookings.Select(b => new BookingHistoryViewModel
            {
                Id = b.Id,
                Date = b.BookingDate,
                TimeSlot = b.TimeSlot,
                PitchName = b.SoccerField?.Name ?? "N/A",
                CustomerName = b.User?.FullName ?? b.User?.UserName ?? "Khách",
                TotalPrice = b.TotalPrice, // Thu đủ 100%
                Status = "Thanh toán đủ",
                IsMatch = false
            }));

            // Thêm các đơn mới đặt cọc vào lịch sử
            historyList.AddRange(depositedBookings.Select(b => new BookingHistoryViewModel
            {
                Id = b.Id,
                Date = b.BookingDate,
                TimeSlot = b.TimeSlot,
                PitchName = b.SoccerField?.Name ?? "N/A",
                CustomerName = (b.User?.FullName ?? b.User?.UserName ?? "Khách") + " (Cọc)",
                TotalPrice = b.DepositAmount, // Chỉ hiển thị số tiền cọc đã thu
                Status = "Tiền cọc 30%",
                IsMatch = false
            }));

            // ==========================================
            // 5. ĐÓNG GÓI VIEWMODEL
            // ==========================================
            var vm = new RevenueDashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalExpense = totalExpense,
                RecentExpenses = expenses,
                // Sắp xếp lịch sử mới nhất lên đầu
                RecentRevenues = historyList.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToList()
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