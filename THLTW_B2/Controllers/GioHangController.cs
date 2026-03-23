using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Helpers; // Dùng phép thuật JSON 
using THLTW_B2.Models;
using Microsoft.AspNetCore.Authorization;
namespace THLTW_B2.Controllers
{
    [Authorize]
    public class GioHangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GioHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        const string CART_KEY = "GI0_HANG"; // Tên của cái giỏ

        // 1. Hàm lấy giỏ hàng hiện tại (Nếu chưa có thì tạo mới)
        public List<CartItem> GetCartItems()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>(CART_KEY);
            if (cart == null)
            {
                cart = new List<CartItem>();
            }
            return cart;
        }

        // 2. Action: Thêm sản phẩm vào giỏ
        // Action: Thêm sản phẩm vào giỏ (Nâng cấp có Số lượng)
        [HttpPost]
        public IActionResult AddToCart(int id, int quantity)
        {
            // Nếu khách lỡ nhập số âm hoặc số 0 thì tự cho bằng 1
            if (quantity <= 0) quantity = 1;

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.ProductId == id);

            // Tìm sản phẩm trong kho
            var product = _context.Products.SingleOrDefault(p => p.ProductId == id);
            if (product != null)
            {
                if (item == null)
                {
                    cart.Add(new CartItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        Price = product.Price,
                        Quantity = quantity, // Thêm số lượng khách nhập
                        ImageUrl = product.ImageUrl ?? "/images/no-image.png"
                    });
                }
                else
                {
                    // Đã có trong giỏ thì cộng dồn số lượng mới
                    item.Quantity += quantity;
                }

                HttpContext.Session.Set(CART_KEY, cart);
                TempData["ThongBao"] = $"Đã thêm {quantity} {product.Name} vào giỏ hàng!";
            }

            return RedirectToAction("Index", "CuaHang");
        }

        // 3. Hàm hiển thị trang Giỏ Hàng
        public IActionResult Index()
        {
            var cart = GetCartItems();
            return View(cart);
        }

        // ==========================================================
        // 4. ĐÂY CHÍNH LÀ HÀM THANH TOÁN (Sếp tìm mãi mới thấy nè!)
        // ==========================================================
        [HttpPost]
        public async Task<IActionResult> ThanhToan(string ghiChu)
        {
            var cart = GetCartItems();
            // Nếu giỏ trống thì quay lại trang giỏ hàng
            if (cart == null || cart.Count == 0) return RedirectToAction("Index");

            // BƯỚC A: TẠO HÓA ĐƠN MỚI
            var order = new Order
            {
                OrderDate = DateTime.Now,
                CustomerName = "Khách Hàng VIP", // Có thể thay bằng User đang đăng nhập sau
                PhoneNumber = "0988888888",
                TotalAmount = cart.Sum(c => c.Total),
                Status = 1, // 1 = Đã thanh toán
                Note = ghiChu // Ghi chú: Giao ra sân số 2...
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Lưu để sinh ra mã Hóa Đơn

            // BƯỚC B: LƯU TỪNG MÓN & TRỪ TỒN KHO
            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };
                _context.OrderDetails.Add(orderDetail);

                // Trừ tồn kho lon Coca
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= item.Quantity; // 100 - 1 = 99
                    _context.Products.Update(product);
                }
            }

            // BƯỚC C: LƯU TẤT CẢ VÀ XÓA GIỎ HÀNG
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove(CART_KEY);

            // Tạm thời báo thành công bằng chữ, sau này sếp thiết kế file HTML đẹp đẹp thay vào nhé
            TempData["ThongBao"] = $"CHỐT ĐƠN THÀNH CÔNG! Nhân viên đang mang đồ ra theo ghi chú: {ghiChu}";

            // Đá khách hàng quay ngược về trang Cửa Hàng
            return RedirectToAction("Index", "CuaHang");
        }
    }
}