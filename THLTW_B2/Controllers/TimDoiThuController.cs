using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;

namespace THLTW_B2.Controllers
{
    [Authorize] // Bảo vệ trang: Yêu cầu đăng nhập
    public class TimDoiThuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TimDoiThuController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager; // Gán giá trị để không bị Null
        }

        // GET: TimDoiThu/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            bool isMember = false;
            string myTeamName = ""; // Biến chứa Tên Đội

            if (currentUser != null)
            {
                // Tìm đội bóng do user này làm Đội trưởng
                var existingTeam = await _context.Teams.FirstOrDefaultAsync(t => t.CaptainId == currentUser.Id);

                if (existingTeam != null)
                {
                    isMember = true;
                    myTeamName = existingTeam.TeamName; // Lấy chuẩn tên Đội đã đăng ký
                }
            }

            ViewBag.IsMember = isMember;
            ViewBag.MyTeamName = myTeamName; // Truyền tên Đội sang giao diện

            var matches = _context.MatchRequests.OrderByDescending(m => m.CreatedAt).ToList();
            return View(matches);
        }

        // POST: TimDoiThu/NhanKeo/5
        [HttpPost]
        public IActionResult NhanKeo(int id)
        {
            // Tìm kèo đấu trong Database dựa vào ID
            var match = _context.MatchRequests.Find(id);

            // Kiểm tra xem kèo có tồn tại và thực sự còn trống hay không
            if (match != null && match.Status == "Đang chờ đối thủ")
            {
                // Cập nhật trạng thái
                match.Status = "Đã có đối thủ";

                // Lưu thay đổi xuống SQL Server
                _context.SaveChanges();
            }

            // Tải lại trang danh sách để giao diện cập nhật ngay lập tức
            return RedirectToAction("Index");
        }

        // API Kiểm tra sân rảnh DÀNH RIÊNG CHO TRANG CÁP KÈO
        [HttpGet]
        public IActionResult GetPitchesStatus(DateTime date, string timeSlot)
        {
            var allPitches = _context.SoccerFields.Where(s => s.IsActive).ToList();

            // 1. Tìm sân đã bị Bao Trọn (Đặt Sân)
            var bookedPitchIds = _context.Bookings
                .Where(b => b.BookingDate.Date == date.Date && b.TimeSlot == timeSlot && b.Status != "Đã hủy")
                .Select(b => b.SoccerFieldId)
                .ToList();

            var bookedPitchNames = _context.SoccerFields
                .Where(s => bookedPitchIds.Contains(s.FieldId))
                .Select(s => s.Name.Trim())
                .ToList();

            // 2. Tìm sân ĐÃ CÓ NGƯỜI TẠO KÈO (MatchRequest)
            var matchedPitchNames = _context.MatchRequests
                .Where(m => m.MatchDate.Date == date.Date &&
                            m.TimeSlot == timeSlot &&
                            !m.Status.Contains("Hủy"))
                .Select(m => m.PitchName.Trim())
                .ToList();

            // 3. Gộp cả 2 danh sách lại
            var allLockedPitches = bookedPitchNames.Concat(matchedPitchNames).ToList();

            // 4. Trả kết quả
            var result = allPitches.Select(p => new {
                name = p.Name,
                price = p.PricePerHour * (decimal)1.5,
                // Bôi xám nếu sân nằm trong "Danh sách đen"
                isBooked = allLockedPitches.Contains(p.Name.Trim())
            }).ToList();

            return Json(result);
        }

        // ==========================================
        // API: LẤY THÔNG SỐ ĐỘI BÓNG ĐỂ HIỂN THỊ MODAL
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetTeamStats(string teamName)
        {
            if (string.IsNullOrEmpty(teamName)) return BadRequest();

            // LÀM SẠCH CHUỖI: Ép về chữ thường và cắt khoảng trắng 2 đầu
            string cleanTeamName = teamName.Trim().ToLower();

            // TÌM KIẾM THÔNG MINH: So sánh chữ thường
            var team = await _context.Teams.FirstOrDefaultAsync(t => t.TeamName.ToLower() == cleanTeamName);

            if (team == null)
            {
                return Json(new { success = false, message = "Đội bóng này chưa đăng ký Hồ sơ Hội viên chính thức." });
            }

            // Tính toán nhanh tỉ lệ thắng
            double winRate = team.TotalMatches == 0 ? 0 : Math.Round((double)team.Wins / team.TotalMatches * 100, 1);

            return Json(new
            {
                success = true,
                teamName = team.TeamName,
                logoUrl = string.IsNullOrEmpty(team.LogoUrl) ? "/images/default-logo.png" : team.LogoUrl,
                description = string.IsNullOrEmpty(team.Description) ? "Đội bóng yêu hòa bình, thích giao lưu." : team.Description,
                totalMatches = team.TotalMatches,
                wins = team.Wins,
                draws = team.Draws,
                losses = team.Losses,
                winRate = winRate,
                fairPlayPoint = team.FairPlayPoint,
                membersList = team.MembersList
            });
        }
    }
}