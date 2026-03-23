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

            // 1. NẾU NHÂN VIÊN BẤM VÀO TAB "TÌM ĐỐI THỦ"
            if (tab == "timdoithu")
            {
                // Lấy dữ liệu từ bảng MatchRequests
                var matchRequests = await _context.MatchRequests
        .OrderByDescending(m => m.CreatedAt)
        .ToListAsync();

                // Gắn vào ViewBag để gửi sang giao diện
                ViewBag.MatchRequests = matchRequests;

                // Trả về danh sách Booking rỗng để model @model không bị lỗi
                return View(new List<Booking>());
            }

            // 2. NẾU LÀ CÁC TAB ĐẶT SÂN BÌNH THƯỜNG (Giữ nguyên code cũ của bạn)
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
            // 1. Lấy danh mục để làm thanh lọc (Sửa 'Categories' thành tên bảng của bạn nếu khác)
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // 2. Lấy danh sách sản phẩm
            var products = await _context.Products.ToListAsync();
            return View(products);
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

            // Bắt đầu một Transaction để đảm bảo: Nếu lỗi ở đâu thì hủy toàn bộ, không bị trừ tiền oan
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal totalAmount = 0;
                var chiTietMua = new List<string>();

                // 1. Duyệt qua từng món hàng để kiểm tra và trừ tồn kho
                foreach (var item in cart)
                {
                    // Lấy sản phẩm từ DB ra (Dùng khóa chính là Id hoặc ProductId tùy model của bạn)
                    var product = await _context.Products.FindAsync(item.Id);

                    if (product == null)
                    {
                        return Json(new { success = false, message = $"Lỗi: Không tìm thấy sản phẩm '{item.Name}'." });
                    }

                    // Chốt chặn Backend: Kẻ gian dùng code bypass JavaScript cũng không mua quá số lượng được
                    if (product.StockQuantity < item.Quantity)
                    {
                        return Json(new { success = false, message = $"Sản phẩm '{item.Name}' chỉ còn {product.StockQuantity} cái trong kho." });
                    }

                    // TRỪ TỒN KHO
                    product.StockQuantity -= item.Quantity;

                    // Tính tiền và ghi chú
                    totalAmount += item.Price * item.Quantity;
                    chiTietMua.Add($"{item.Quantity}x {item.Name}");
                }

                // 2. Tạo hóa đơn thanh toán
                var newPayment = new Payment
                {
                    InvoiceCode = "POS-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Amount = totalAmount,
                    PaymentMethod = "Tiền mặt",
                    Status = "Thành công",
                    PaymentDate = DateTime.Now,
                    Note = "Bán lẻ: " + string.Join(", ", chiTietMua)
                };

                _context.Payments.Add(newPayment);

                // 3. Lưu toàn bộ thay đổi (Trừ kho + Thêm hóa đơn)
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // Có lỗi xảy ra thì hoàn tác lại kho
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}