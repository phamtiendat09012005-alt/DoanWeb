using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Cần thêm cái này để dùng SelectList
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using THLTW_B2.Models;

namespace THLTW_B2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // 1. Hiển thị danh sách nhân viên
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userWithRoles = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userWithRoles.Add(new UserListViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles.ToList()
                });
            }
            return View(userWithRoles);
        }

        // 2. Giao diện Cấp tài khoản nhân viên (GET)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách Role từ Database để nạp vào Dropdown
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.RoleList = new SelectList(roles);

            return View();
        }

        // 3. Xử lý lưu tài khoản nhân viên (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Bước A: Tạo đối tượng User
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    EmailConfirmed = true // Tự động xác nhận email cho nhân viên
                };

                // Bước B: Lưu vào Database kèm mật khẩu
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Bước C: Gán quyền (Role) đã chọn cho User vừa tạo
                    if (!string.IsNullOrEmpty(model.SelectedRole))
                    {
                        await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    }

                    return RedirectToAction(nameof(Index));
                }

                // Nếu có lỗi (ví dụ mật khẩu không đủ độ khó), hiển thị ra màn hình
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Nếu thất bại, nạp lại danh sách Role để không bị lỗi Dropdown khi load lại trang
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.RoleList = new SelectList(roles);
            return View(model);
        }
    }

    // --- VIEW MODELS ---
   

    public class CreateEmployeeViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải từ {2} ký tự trở lên", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu tạm thời")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn quyền")]
        [Display(Name = "Vai trò hệ thống")]
        public string SelectedRole { get; set; }
    }
}