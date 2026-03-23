using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;

namespace THLTW_B2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PayrollController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. HIỂN THỊ BẢNG LƯƠNG TẠM TÍNH (Những ca chưa thanh toán)
        public async Task<IActionResult> Index()
        {
            // Lấy các ca đã đi làm (IsAttended = true) nhưng chưa trả lương (IsPaid = false)
            var unpaidShifts = await _context.EmployeeShifts
                .Include(s => s.User)
                .Where(s => s.IsAttended == true && s.IsPaid == false)
                .ToListAsync();

            // Gom nhóm theo từng nhân viên để tính tổng
            var payrollList = unpaidShifts.GroupBy(s => s.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    FullName = g.First().User.FullName ?? g.First().User.UserName,
                    TotalShifts = g.Count(),
                    TotalSalary = g.Sum(s => s.ShiftWage)
                }).ToList();

            // Tính quỹ lương dự kiến chuẩn bị xuất
            ViewBag.GrandTotal = payrollList.Sum(p => p.TotalSalary);

            return View(payrollList);
        }

        // 2. CHỐT LƯƠNG & GHI NHẬN VÀO CHI PHÍ DOANH THU
        [HttpPost]
        public async Task<IActionResult> ChotLuong()
        {
            // Lấy lại các ca chưa trả lương
            var unpaidShifts = await _context.EmployeeShifts
                .Where(s => s.IsAttended == true && s.IsPaid == false)
                .ToListAsync();

            if (!unpaidShifts.Any())
            {
                TempData["ErrorMessage"] = "Không có ca làm việc nào cần thanh toán!";
                return RedirectToAction("Index");
            }

            // Tính tổng tiền cần chi
            decimal tongTienChi = unpaidShifts.Sum(s => s.ShiftWage);

            // A. ĐÁNH DẤU CÁC CA NÀY LÀ "ĐÃ THANH TOÁN"
            foreach (var shift in unpaidShifts)
            {
                shift.IsPaid = true;
            }

            // B. TẠO TỰ ĐỘNG PHIẾU CHI (EXPENSE) CHO ADMIN QUẢN LÝ DOANH THU
            var chiPhiLuong = new Expense
            {
                ExpenseDate = DateTime.Now,
                Amount = tongTienChi,

                Title = "Thanh toán quỹ lương nhân viên",
                Category = "Lương",
                Note = $"Chi lương cho {unpaidShifts.Select(s => s.UserId).Distinct().Count()} nhân viên (Tổng cộng: {unpaidShifts.Count} ca làm việc)"
            };

            _context.Expenses.Add(chiPhiLuong);

            // Lưu tất cả vào Database cùng lúc
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã chốt lương thành công! Khoản chi {tongTienChi:N0}đ đã được tự động thêm vào Báo cáo doanh thu.";
            return RedirectToAction("Index");
        }

        // 3. HIỂN THỊ FORM PHÂN CA LÀM VIỆC (GET)
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.UserId = new SelectList(_context.Users.ToList(), "Id", "UserName");
            return View();
        }

        // 4. XỬ LÝ LƯU CA LÀM VIỆC VÀO DATABASE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeShift model)
        {
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                model.IsAttended = true;
                model.IsPaid = false;

                _context.EmployeeShifts.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã phân ca làm việc thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.UserId = new SelectList(_context.Users.ToList(), "Id", "UserName", model.UserId);
            return View(model);
        }
        // XÓA CA LÀM VIỆC BỊ NHẦM
        [HttpPost]
        public async Task<IActionResult> DeleteShift(int id)
        {
            var shift = await _context.EmployeeShifts.FindAsync(id);
            if (shift != null)
            {
                // Chỉ cho phép xóa ca CHƯA THANH TOÁN
                if (shift.IsPaid)
                {
                    return Json(new { success = false, message = "Không thể xóa ca đã được thanh toán!" });
                }

                _context.EmployeeShifts.Remove(shift);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy ca làm việc này!" });
        }
        // ==========================================
        // THÊM MỚI: 5. HÀM HIỂN THỊ DANH SÁCH CHI TIẾT CA LÀM (BẢNG EXCEL)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> DanhSachCaLam()
        {
            // Lấy toàn bộ lịch sử ca làm việc, hiển thị ngày mới nhất lên đầu tiên
            var shifts = await _context.EmployeeShifts
                .Include(s => s.User)
                .OrderByDescending(s => s.WorkDate)
                .ToListAsync();

            return View(shifts);
        }
        // ==========================================
        // 6. HIỂN THỊ BẢNG CHẤM CÔNG DẠNG EXCEL MATRIX
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> BangChamCong(int? thang, int? nam)
        {
            int m = thang ?? DateTime.Now.Month;
            int y = nam ?? DateTime.Now.Year;

            ViewBag.Thang = m;
            ViewBag.Nam = y;
            ViewBag.SoNgayTrongThang = DateTime.DaysInMonth(y, m);

            // Lấy danh sách tất cả nhân viên
            ViewBag.DanhSachNhanVien = await _context.Users.ToListAsync();

            // Lấy toàn bộ ca làm việc của tháng đó
            var caLams = await _context.EmployeeShifts
                .Where(s => s.WorkDate.Month == m && s.WorkDate.Year == y)
                .ToListAsync();

            return View(caLams);
        }

        // 7. LƯU CA LÀM BẰNG AJAX (KHI CLICK VÀO Ô TRONG BẢNG EXCEL)
        [HttpPost]
        public async Task<IActionResult> LuuCaLamAjax(string userId, int day, int month, int year, string shiftName, decimal shiftWage)
        {
            var date = new DateTime(year, month, day);

            // Tìm xem ngày hôm đó nhân viên này đã có ca chưa
            var existingShift = await _context.EmployeeShifts
                .FirstOrDefaultAsync(s => s.UserId == userId && s.WorkDate.Date == date.Date);

            if (string.IsNullOrEmpty(shiftName))
            {
                // Nếu chọn "Nghỉ / Xóa ca" thì xóa khỏi DB
                if (existingShift != null)
                {
                    _context.EmployeeShifts.Remove(existingShift);
                }
            }
            else
            {
                if (existingShift != null)
                {
                    // Cập nhật ca cũ
                    existingShift.ShiftName = shiftName;
                    existingShift.ShiftWage = shiftWage;
                }
                else
                {
                    // Thêm ca mới
                    _context.EmployeeShifts.Add(new EmployeeShift
                    {
                        UserId = userId,
                        WorkDate = date,
                        ShiftName = shiftName,
                        ShiftWage = shiftWage,
                        IsAttended = true,
                        IsPaid = false
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}