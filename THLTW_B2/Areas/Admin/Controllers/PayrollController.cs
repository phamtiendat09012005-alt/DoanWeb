using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public PayrollController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. HIỂN THỊ BẢNG LƯƠNG TẠM TÍNH (Những ca chưa thanh toán)
        public async Task<IActionResult> Index()
        {
            var unpaidShifts = await _context.EmployeeShifts
                .Include(s => s.User)
                .Where(s => s.IsAttended == true && s.IsPaid == false)
                .ToListAsync();

            var payrollList = unpaidShifts.GroupBy(s => s.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    FullName = g.First().User.FullName ?? g.First().User.UserName,
                    TotalShifts = g.Count(),
                    TotalSalary = g.Sum(s => s.ShiftWage)
                }).ToList();

            ViewBag.GrandTotal = payrollList.Sum(p => p.TotalSalary);
            return View(payrollList);
        }

        // 2. CHỐT LƯƠNG & GHI NHẬN VÀO CHI PHÍ DOANH THU
        [HttpPost]
        public async Task<IActionResult> ChotLuong()
        {
            var unpaidShifts = await _context.EmployeeShifts
                .Where(s => s.IsAttended == true && s.IsPaid == false)
                .ToListAsync();

            if (!unpaidShifts.Any())
            {
                TempData["ErrorMessage"] = "Không có ca làm việc nào cần thanh toán!";
                return RedirectToAction("Index");
            }

            decimal tongTienChi = unpaidShifts.Sum(s => s.ShiftWage);

            foreach (var shift in unpaidShifts)
            {
                shift.IsPaid = true;
            }

            var chiPhiLuong = new Expense
            {
                ExpenseDate = DateTime.Now,
                Amount = tongTienChi,
                Title = "Thanh toán quỹ lương nhân viên",
                Category = "Lương",
                Note = $"Chi lương cho {unpaidShifts.Select(s => s.UserId).Distinct().Count()} nhân viên (Tổng cộng: {unpaidShifts.Count} ca làm việc)"
            };

            _context.Expenses.Add(chiPhiLuong);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã chốt lương thành công! Khoản chi {tongTienChi:N0}đ đã được tự động thêm vào Báo cáo doanh thu.";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 3. HIỂN THỊ FORM PHÂN CA LÀM VIỆC (GET) - ĐÃ SỬA LỌC
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create() // Nhớ đổi thành async Task nhé
        {
            // Chỉ lấy những ai có role là "Employee"
            var employees = await _userManager.GetUsersInRoleAsync("Employee");

            // Ưu tiên hiển thị FullName, nếu không có mới lấy UserName
            var selectList = employees.Select(e => new
            {
                Id = e.Id,
                DisplayName = !string.IsNullOrEmpty(e.FullName) ? e.FullName : e.UserName
            });

            ViewBag.UserId = new SelectList(selectList, "Id", "DisplayName");
            return View();
        }

        // ==========================================
        // 4. XỬ LÝ LƯU CA LÀM VIỆC VÀO DATABASE (POST) - ĐÃ SỬA LỌC
        // ==========================================
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

            // Nếu nhập lỗi, nạp lại đúng danh sách Employee để Dropdown không bị tàng hình
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            var selectList = employees.Select(e => new
            {
                Id = e.Id,
                DisplayName = !string.IsNullOrEmpty(e.FullName) ? e.FullName : e.UserName
            });

            ViewBag.UserId = new SelectList(selectList, "Id", "DisplayName", model.UserId);
            return View(model);
        }

        // XÓA CA LÀM VIỆC BỊ NHẦM
        [HttpPost]
        public async Task<IActionResult> DeleteShift(int id)
        {
            var shift = await _context.EmployeeShifts.FindAsync(id);
            if (shift != null)
            {
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

        // 5. HÀM HIỂN THỊ DANH SÁCH CHI TIẾT CA LÀM (BẢNG EXCEL)
        [HttpGet]
        public async Task<IActionResult> DanhSachCaLam()
        {
            var shifts = await _context.EmployeeShifts
                .Include(s => s.User)
                .OrderByDescending(s => s.WorkDate)
                .ToListAsync();

            return View(shifts);
        }

        // 6. HIỂN THỊ BẢNG CHẤM CÔNG DẠNG EXCEL MATRIX
        [HttpGet]
        public async Task<IActionResult> BangChamCong(int? thang, int? nam)
        {
            int m = thang ?? DateTime.Now.Month;
            int y = nam ?? DateTime.Now.Year;

            ViewBag.Thang = m;
            ViewBag.Nam = y;
            ViewBag.SoNgayTrongThang = DateTime.DaysInMonth(y, m);

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            var danhSachNhanVien = admins.Union(employees).ToList();

            ViewBag.DanhSachNhanVien = danhSachNhanVien;

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

            var existingShift = await _context.EmployeeShifts
                .FirstOrDefaultAsync(s => s.UserId == userId && s.WorkDate.Date == date.Date);

            if (string.IsNullOrEmpty(shiftName))
            {
                if (existingShift != null)
                {
                    _context.EmployeeShifts.Remove(existingShift);
                }
            }
            else
            {
                if (existingShift != null)
                {
                    existingShift.ShiftName = shiftName;
                    existingShift.ShiftWage = shiftWage;
                }
                else
                {
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