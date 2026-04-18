using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.Models;

namespace THLTW_B2.Areas.Admin.Controllers
{
    // Đảm bảo class CreateEmployeeViewModel có tồn tại, nếu bạn để ở Models thì đổi namespace tương ứng
   

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
        // 1. TRANG DANH SÁCH
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var viewModelList = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

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
        // 2. TẠO TÀI KHOẢN (Đã fix đồng bộ View - Controller)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            // Lần đầu vào trang: Nạp danh sách quyền cho Dropdown
            ViewBag.RoleList = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Lấy quyền từ giao diện người dùng chọn (nếu không chọn thì mặc định là Employee)
                    string roleToAssign = string.IsNullOrEmpty(model.SelectedRole) ? "Employee" : model.SelectedRole;

                    if (!await _roleManager.RoleExistsAsync(roleToAssign))
                        await _roleManager.CreateAsync(new IdentityRole(roleToAssign));

                    // Add đúng cái quyền vừa chọn
                    await _userManager.AddToRoleAsync(user, roleToAssign);

                    TempData["SuccessMessage"] = $"Đã tạo thành công tài khoản cho: {model.FullName}";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            // BẢN VÁ LỖI: Nạp lại ViewBag nếu form bị lỗi nhập liệu
            ViewBag.RoleList = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
            return View(model);
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
        // 4. POST: LƯU QUYỀN
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
                    if (currentRoles.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    }

                    await _userManager.AddToRoleAsync(user, model.RoleName);
                }

                TempData["SuccessMessage"] = $"Cập nhật quyền cho {user.UserName} thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // ==========================================
        // 5. POST: XÓA TÀI KHOẢN
        // ==========================================
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

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["ErrorMessage"] = "LỖI: Bạn không thể tự thu hồi tài khoản của chính mình khi đang đăng nhập!";
                return RedirectToAction("Index");
            }

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