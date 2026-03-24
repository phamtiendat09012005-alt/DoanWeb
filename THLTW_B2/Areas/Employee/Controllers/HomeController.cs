using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace THLTW_B2.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(YeuCauDatSan));
        }

        // ==========================================
        // 1. QUẢN LÝ ĐẶT SÂN (LÚC CHẠY HOÀN HẢO NHẤT)
        // ==========================================

        public async Task<IActionResult> YeuCauDatSan(string tab = "donngay")
        {
            ViewBag.CurrentTab = tab;

            if (tab == "timdoithu")
            {
                var matchRequests = await _context.MatchRequests
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();
                ViewBag.MatchRequests = matchRequests;
                return View(new List<Booking>());
            }

            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.SoccerField)
                .AsQueryable();

            if (tab == "donngay")
            {
                query = query.Where(b => b.Status == "Chờ duyệt" || b.Status == "Đã xác nhận" || b.Status == "Đã cọc");
            }
            else if (tab == "doncodinh")
            {
                query = query.Where(b => b.Status == "Đơn cố định");
            }

            var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanDon(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null && (booking.Status == "Chờ duyệt" || booking.Status == "Đã cọc"))
            {
                booking.Status = "Đã xác nhận";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã XÁC NHẬN đơn đặt sân! Chờ khách đến đá.";
            }
            return RedirectToAction(nameof(YeuCauDatSan));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetDon(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null && booking.Status != "Đã hoàn thành")
            {
                booking.Status = "Đã hoàn thành";
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đơn đã HOÀN THÀNH.";
            }
            return RedirectToAction(nameof(YeuCauDatSan));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDon(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                booking.Status = "Đã hủy";
                await _context.SaveChangesAsync();
                TempData["Error"] = "Đã HỦY yêu cầu đặt sân.";
            }
            return RedirectToAction(nameof(YeuCauDatSan));
        }

        // ==========================================
        // 2. CHỨC NĂNG TẠO MỚI LỊCH ĐẶT SÂN
        // ==========================================

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.ListSan = _context.SoccerFields.ToList();
            ViewBag.ListKhach = _context.Users.ToList();
            return View(new Booking());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            booking.Status = "Đã cọc";
            booking.DepositAmount = booking.TotalPrice * (decimal)0.3; // Tự tính cọc 30%
            booking.CreatedAt = DateTime.Now;

            ModelState.Remove("SoccerField");
            ModelState.Remove("User");

            if (booking.UserId == "GUEST")
            {
                var guestUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "khachtudo");
                if (guestUser == null)
                {
                    guestUser = new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = "khachtudo",
                        Email = "khachtudo@hethong.com",
                        FullName = "Khách Tự Do (Tại Quầy)",
                        PhoneNumber = "0000000000",
                        EmailConfirmed = true
                    };
                    _context.Users.Add(guestUser);
                    await _context.SaveChangesAsync();
                }
                booking.UserId = guestUser.Id;
            }

            if (ModelState.IsValid)
            {
                bool isBooked = await _context.Bookings.AnyAsync(b =>
                    b.SoccerFieldId == booking.SoccerFieldId &&
                    b.BookingDate.Date == booking.BookingDate.Date &&
                    b.TimeSlot == booking.TimeSlot &&
                    b.Status != "Đã hủy");

                if (isBooked)
                {
                    ModelState.AddModelError("", "LỖI: Sân này ở khung giờ này đã có người đặt trên mạng rồi!");
                    ViewBag.ListSan = _context.SoccerFields.ToList();
                    ViewBag.ListKhach = _context.Users.ToList();
                    return View(booking);
                }

                var pitch = await _context.SoccerFields.FindAsync(booking.SoccerFieldId);
                var chotKeo = await _context.MatchRequests.AnyAsync(m =>
                    m.PitchName == pitch.Name &&
                    m.MatchDate.Date == booking.BookingDate.Date &&
                    m.TimeSlot == booking.TimeSlot &&
                    m.Status != "Đang chờ đối thủ" && m.Status != "Hết hạn (Chờ hoàn cọc)");

                if (chotKeo)
                {
                    ModelState.AddModelError("", "LỖI: Sân này đã có đội cáp kèo chốt đá rồi!");
                    ViewBag.ListSan = _context.SoccerFields.ToList();
                    ViewBag.ListKhach = _context.Users.ToList();
                    return View(booking);
                }

                var pendingMatches = await _context.MatchRequests.Where(m =>
                    m.MatchDate.Date == booking.BookingDate.Date &&
                    m.TimeSlot == booking.TimeSlot &&
                    m.OpponentName == null &&
                    m.PitchName == pitch.Name).ToListAsync();

                foreach (var match in pendingMatches)
                {
                    match.Status = "Đã hủy (Chờ hoàn cọc)";
                    match.Note = $"Hệ thống tự hủy lúc {DateTime.Now.ToString("HH:mm")}: Nhân viên quán đã chốt cho khách lẻ.";
                }

                try
                {
                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Đã chốt sân cho khách!";
                    return RedirectToAction(nameof(YeuCauDatSan));
                }
                catch (Exception ex)
                {
                    string chiTietLoi = ex.Message;
                    if (ex.InnerException != null) chiTietLoi += " | LỖI: " + ex.InnerException.Message;
                    ModelState.AddModelError("", "Lỗi khi lưu DB: " + chiTietLoi);
                }
            }

            ViewBag.ListSan = _context.SoccerFields.ToList();
            ViewBag.ListKhach = _context.Users.ToList();
            return View(booking);
        }

        // ==========================================
        // 3. CHỨC NĂNG BÁN HÀNG DỊCH VỤ TẠI QUẦY (TRẢ VỀ NGUYÊN BẢN CHỈ TRỪ KHO)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> BanHang()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            var products = await _context.Products.ToListAsync();
            return View(products);
        }

        public class CartItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> XuLyThanhToanPOS([FromBody] List<CartItem> cart)
        {
            if (cart == null || cart.Count == 0) return Json(new { success = false, message = "Giỏ hàng rỗng!" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal totalAmount = 0;
                var chiTietMua = new List<string>();

                // 1. VÒNG LẶP TRỪ TỒN KHO
                foreach (var item in cart)
                {
                    var product = await _context.Products.FindAsync(item.Id);
                    if (product == null) return Json(new { success = false, message = "Lỗi sản phẩm." });
                    if (product.StockQuantity < item.Quantity) return Json(new { success = false, message = "Không đủ tồn kho." });

                    product.StockQuantity -= item.Quantity; // Trừ kho
                    totalAmount += item.Price * item.Quantity; // Cộng dồn tiền
                    chiTietMua.Add($"{item.Quantity}x {item.Name}");
                }

                // =========================================================
                // 2. TUYỆT CHIÊU: TẠO "ĐƠN ĐẶT SÂN ẢO" ĐỂ ADMIN CỘNG TIỀN NƯỚC
                // =========================================================

                // Lấy đại ID của Sân bóng đầu tiên trong hệ thống để SQL không chửi lỗi "Thiếu Sân"
                var defaultField = await _context.SoccerFields.FirstOrDefaultAsync();
                int fieldId = defaultField != null ? defaultField.FieldId : 0;

                if (fieldId > 0)
                {
                    // Lấy hoặc tạo tài khoản "Khách Tự Do"
                    var guestUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "khachtudo");
                    if (guestUser == null)
                    {
                        guestUser = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "khachtudo", Email = "khachtudo@hethong.com", FullName = "Khách Tự Do (Tại Quầy)", PhoneNumber = "0000000000", EmailConfirmed = true };
                        _context.Users.Add(guestUser);
                        await _context.SaveChangesAsync();
                    }

                    // Nhét tiền bán nước vào vỏ bọc "Đơn đặt sân"
                    var fakeBooking = new Booking
                    {
                        UserId = guestUser.Id,
                        SoccerFieldId = fieldId,
                        BookingDate = DateTime.Now.Date,
                        TimeSlot = "POS " + DateTime.Now.ToString("HH:mm"), // Ghi nhận đây là đơn POS thay vì giờ đá
                        TotalPrice = totalAmount,
                        DepositAmount = totalAmount, // Nộp đủ 100% tiền nước
                        Status = "Đã hoàn thành", // Khớp mật khẩu để Admin hút tiền lên Báo cáo
                        CreatedAt = DateTime.Now,
                        Note = "Bán lẻ tại quầy: " + string.Join(", ", chiTietMua)
                    };

                    _context.Bookings.Add(fakeBooking);
                }

                // 3. LƯU TẤT CẢ VÀO DATABASE
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}