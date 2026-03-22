using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.Models;

namespace THLTW_B2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmployeeController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ==========================================
        // 1. TRANG DANH SÁCH: Đã mở bộ lọc để "cứu" dữ liệu
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var viewModelList = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // ĐÃ BỎ ĐIỀU KIỆN IF Ở ĐÂY ĐỂ BẠN NHÌN THẤY TẤT CẢ TÀI KHOẢN!
                viewModelList.Add(new UserListViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles.ToList()
                });
            }
            return View(viewModelList);
        }

        // ==========================================
        // 2. TẠO TÀI KHOẢN (Giữ nguyên, code bạn đã chuẩn)
        // ==========================================
        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(string fullName, string email, string password)
        {
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("Employee"))
                    await _roleManager.CreateAsync(new IdentityRole("Employee"));

                await _userManager.AddToRoleAsync(user, "Employee");
                TempData["SuccessMessage"] = $"Đã tạo thành công tài khoản cho: {fullName}";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View();
        }

        // ==========================================
        // 3. GET: HIỂN THỊ FORM SỬA QUYỀN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> EditRoles(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var model = new EditRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                RoleName = userRoles.FirstOrDefault() ?? "Employee"
            };
            return View(model);
        }

        // ==========================================
        // 4. POST: LƯU QUYỀN (Đã fix lỗi sập trang)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(EditRolesViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null) return NotFound();

                var currentRoles = await _userManager.GetRolesAsync(user);

                if (!currentRoles.Contains(model.RoleName))
                {
                    // FIX LỖI: Chỉ xóa nếu user đang có quyền, tránh lỗi sập Identity
                    if (currentRoles.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    }

                    // Thêm quyền mới
                    await _userManager.AddToRoleAsync(user, model.RoleName);
                }

                TempData["SuccessMessage"] = $"Cập nhật quyền cho {user.UserName} thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Không tìm thấy ID tài khoản cần xóa.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Tài khoản không tồn tại hoặc đã bị xóa trước đó.";
                return RedirectToAction("Index");
            }

            // LỚP GIÁP BẢO VỆ: Ngăn Admin tự xóa chính mình (tự sát)
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["ErrorMessage"] = "LỖI: Bạn không thể tự thu hồi tài khoản của chính mình khi đang đăng nhập!";
                return RedirectToAction("Index");
            }

            // Thực thi lệnh xóa khỏi Database
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn tài khoản {user.UserName} thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi hệ thống xảy ra khi cố gắng xóa tài khoản này.";
            }

            return RedirectToAction("Index");
        }
    }
}