using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        // 1. QUẢN LÝ ĐẶT SÂN
        // ==========================================

        public async Task<IActionResult> YeuCauDatSan(string tab = "donngay")
        {
            ViewBag.CurrentTab = tab;

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

                var newPayment = new Payment
                {
                    InvoiceCode = "HD-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999),
                    BookingId = booking.Id,
                    Amount = booking.TotalPrice,
                    PaymentMethod = "Tiền mặt",
                    Status = "Thành công",
                    PaymentDate = DateTime.Now,
                    Note = "Thu tiền sân " + booking.TimeSlot
                };

                _context.Payments.Add(newPayment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đơn đã HOÀN THÀNH. Hệ thống đã cộng tiền vào Doanh thu!";
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
        [HttpPost]
        [HttpPost]
     
        // ==========================================
        // 2. CHỨC NĂNG TẠO MỚI LỊCH ĐẶT SÂN
        // ==========================================

        [HttpGet]
        public IActionResult Create()
        {
            // BẢN VÁ 1: Thêm .ToList() để nạp dữ liệu chắc chắn 100% không bị Null
            ViewBag.UserId = new SelectList(_context.Users.ToList(), "Id", "UserName");
            ViewBag.SoccerFieldId = new SelectList(_context.SoccerFields.ToList(), "Id", "Name");

            // BẢN VÁ 2: Phải truyền "new Booking()" sang để View có cái mà bám vào
            return View(new Booking());
        }

        // ==========================================
        // KHU VỰC XỬ LÝ API CHO TRANG BÁN HÀNG
        // ==========================================

        // 1. Tạo một Class nhỏ để hứng dữ liệu từ JavaScript gửi lên
        public class CartItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
        }

        // 2. Hàm nhận dữ liệu và lưu Database
        [HttpPost]
        public async Task<IActionResult> XuLyThanhToanPOS([FromBody] List<CartItem> cart)
        {
            if (cart == null || cart.Count == 0)
            {
                return Json(new { success = false, message = "Giỏ hàng rỗng!" });
            }

            try
            {
                // Tính tổng tiền của cả hóa đơn
                decimal totalAmount = cart.Sum(item => item.Price * item.Quantity);

                // Tạo chuỗi ghi chú liệt kê các món đã mua (VD: "2x Nước suối, 1x Bò húc")
                var chiTietMua = string.Join(", ", cart.Select(c => $"{c.Quantity}x {c.Name}"));

                // Tạo hóa đơn mới lưu vào bảng Payment (dựa theo model Payment của bạn)
                var newPayment = new Payment
                {
                    InvoiceCode = "POS-" + DateTime.Now.ToString("yyyyMMddHHmmss"), // Mã HD Bán lẻ
                    Amount = totalAmount,
                    PaymentMethod = "Tiền mặt",
                    Status = "Thành công",
                    PaymentDate = DateTime.Now,
                    Note = "Bán lẻ tại quầy: " + chiTietMua
                };

                _context.Payments.Add(newPayment);
                await _context.SaveChangesAsync();

                // Trả về thông báo thành công cho JavaScript
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Nếu có lỗi CSDL thì báo về cho giao diện
                return Json(new { success = false, message = ex.Message });
            }
        }
        // ==========================================
        // 3. CHỨC NĂNG BÁN HÀNG DỊCH VỤ TẠI QUẦY (POS)
        // ==========================================


        [HttpGet]
        public async Task<IActionResult> BanHang() // <--- SỬA LẠI DÒNG NÀY LÀ XONG
        {
            var danhSachSanPham = await _context.Products.ToListAsync();
            return View(danhSachSanPham);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            ModelState.Remove("User");
            ModelState.Remove("SoccerField");

            if (ModelState.IsValid)
            {
                booking.CreatedAt = DateTime.Now;
                booking.Status = "Đã xác nhận";

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã tạo thành công lịch đặt sân mới!";
                return RedirectToAction(nameof(YeuCauDatSan));
            }

            // BẢN VÁ 3: Đổi "FullName" thành "UserName" cho đồng bộ với hàm GET bên trên
            ViewBag.UserId = new SelectList(_context.Users.ToList(), "Id", "UserName", booking.UserId);
            ViewBag.SoccerFieldId = new SelectList(_context.SoccerFields.ToList(), "Id", "Name", booking.SoccerFieldId);

            return View(booking);
        }

    }
}