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

        // ==========================================
        // 2. CHỨC NĂNG TẠO MỚI LỊCH ĐẶT SÂN
        // ==========================================

        [HttpGet]
        public IActionResult Create()
        {
            // ĐỔI TÊN THÀNH UserList và SoccerFieldList
            ViewBag.ListSan = _context.SoccerFields.ToList();
            ViewBag.ListKhach = _context.Users.ToList();

            return View(new Booking());

       
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            booking.Status = "Đã xác nhận"; // Nhân viên tự đặt thì mặc định là đã chốt
            booking.CreatedAt = DateTime.Now;
            ModelState.Remove("SoccerField");
            ModelState.Remove("User");

            // 2. Kiểm tra xem dữ liệu hợp lệ không
            if (ModelState.IsValid)
            {
                try
                {
                    // Lưu vào Database
                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    // Hiện thông báo màu xanh báo thành công
                    TempData["Success"] = "Đã tạo lịch đặt sân mới thành công!";

                    // Lưu xong thì quay lại trang danh sách (YeuCauDatSan)
                    return RedirectToAction(nameof(YeuCauDatSan));
                }
                catch (Exception ex)
                {
                    // Báo lỗi nếu Database trục trặc
                    ModelState.AddModelError("", "Lỗi khi lưu dữ liệu: " + ex.Message);
                }
            }

            // 3. NẾU FORM BỊ LỖI (Ví dụ: chưa nhập tiền), BẮT BUỘC PHẢI NẠP LẠI DỮ LIỆU CHO DROPDOWN
            // Nếu không có 2 dòng này, lúc form báo lỗi Dropdown sẽ lại trống trơn
            ViewBag.ListSan = _context.SoccerFields.ToList();
            ViewBag.ListKhach = _context.Users.ToList();

            return View(booking);
        }

        // ==========================================
        // 3. CHỨC NĂNG BÁN HÀNG DỊCH VỤ TẠI QUẦY (POS)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> BanHang()
        {
            var danhSachSanPham = await _context.Products.ToListAsync();
            return View(danhSachSanPham);
        }

        // ==========================================
        // 4. API CHO GIAO DIỆN BÁN HÀNG BẰNG JAVASCRIPT
        // ==========================================

        // Class hứng dữ liệu
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
            if (cart == null || cart.Count == 0)
            {
                return Json(new { success = false, message = "Giỏ hàng rỗng!" });
            }

            try
            {
                decimal totalAmount = cart.Sum(item => item.Price * item.Quantity);
                var chiTietMua = string.Join(", ", cart.Select(c => $"{c.Quantity}x {c.Name}"));

                var newPayment = new Payment
                {
                    InvoiceCode = "POS-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Amount = totalAmount,
                    PaymentMethod = "Tiền mặt",
                    Status = "Thành công",
                    PaymentDate = DateTime.Now,
                    Note = "Bán lẻ tại quầy: " + chiTietMua
                };

                _context.Payments.Add(newPayment);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}