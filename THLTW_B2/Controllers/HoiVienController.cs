using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess; // Thay bằng thư mục chứa ApplicationDbContext của bạn
using THLTW_B2.Models;

namespace THLTW_B2.Controllers
{
    [Authorize] // Bắt buộc phải có tài khoản mới được đăng ký Hội viên
    public class HoiVienController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env; // Dùng để lấy đường dẫn lưu file ảnh

        public HoiVienController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // 1. GET: Hiển thị form đăng ký
        [HttpGet]
        public async Task<IActionResult> DangKy()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Kiểm tra xem user này đã là Đội trưởng của đội nào chưa
            var existingTeam = await _context.Teams.FirstOrDefaultAsync(t => t.CaptainId == user.Id);
            if (existingTeam != null)
            {
                // Nếu đã có đội rồi thì chuyển hướng sang trang Hồ sơ đội bóng (chúng ta sẽ làm sau)
                TempData["InfoMessage"] = "Bạn đã đăng ký Hội viên rồi!";
                return RedirectToAction("Index", "TimDoiThu");
            }

            return View();
        }

        // 2. POST: Xử lý lưu dữ liệu và Upload Logo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(TeamRegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                // Kiểm tra trùng tên đội bóng (không cho phép 2 đội trùng tên)
                if (await _context.Teams.AnyAsync(t => t.TeamName.ToLower() == model.TeamName.ToLower()))
                {
                    ModelState.AddModelError("TeamName", "Tên đội bóng này đã được đăng ký. Vui lòng chọn tên khác!");
                    return View(model);
                }

                string logoPath = "/images/default-logo.png"; // Ảnh mặc định nếu không upload

                // Xử lý Upload file ảnh
                if (model.LogoFile != null && model.LogoFile.Length > 0)
                {
                    // Tạo thư mục wwwroot/uploads/logos nếu chưa có
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "logos");
                    Directory.CreateDirectory(uploadsFolder);

                    // Đổi tên file để không bị trùng (dùng GUID)
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.LogoFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.LogoFile.CopyToAsync(fileStream);
                    }

                    logoPath = "/uploads/logos/" + uniqueFileName;
                }

                // Lưu vào Database
                var newTeam = new Team
                {
                    TeamName = model.TeamName,
                    MembersList = model.MembersList,
                    Description = model.Description,
                    LogoUrl = logoPath,
                    CaptainId = user.Id
                };

                _context.Teams.Add(newTeam);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Chúc mừng! Bạn đã đăng ký Hội viên thành công. Bây giờ bạn có thể tham gia hệ thống Cáp Kèo!";
                return RedirectToAction("Index", "TimDoiThu");
            }

            return View(model);
        }
    }
}