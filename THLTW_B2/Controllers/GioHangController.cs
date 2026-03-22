using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Helpers; // Dùng phép thuật JSON vừa tạo
using THLTW_B2.Models;

namespace THLTW_B2.Controllers
{
    public class GioHangController : Controller
    {
        private readonly ApplicationDbContext _context; // Đổi tên cho khớp với DbContext của sếp

        public GioHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        const string CART_KEY = "GI0_HANG"; // Tên của cái giỏ

        // Hàm lấy giỏ hàng hiện tại (Nếu chưa có thì tạo mới)
        public List<CartItem> GetCartItems()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>(CART_KEY);
            if (cart == null)
            {
                cart = new List<CartItem>();
            }
            return cart;
        }

        // Action: Thêm sản phẩm vào giỏ
        public IActionResult AddToCart(int id)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item == null)
            {
                // Nếu món này chưa có trong giỏ -> Tìm trong DB và thêm vào giỏ với số lượng 1
                var product = _context.Products.SingleOrDefault(p => p.ProductId == id);
                if (product != null)
                {
                    cart.Add(new CartItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        Price = product.Price,
                        Quantity = 1,
                        ImageUrl = product.ImageUrl ?? "/images/no-image.png"
                    });
                }
            }
            else
            {
                // Nếu đã có sẵn lon Coca trong giỏ rồi -> Tăng số lượng lên
                item.Quantity++;
            }

            // Lưu lại giỏ hàng vào Session
            HttpContext.Session.Set(CART_KEY, cart);

            // Thêm xong thì quay lại trang Cửa Hàng
            return RedirectToAction("Index", "CuaHang");
        }
        public IActionResult Index()
        {
            // Lấy cái giỏ từ Session ra
            var cart = GetCartItems();

            // Ném cái giỏ ra ngoài View cho khách xem
            return View(cart);
        }
    }
}