using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess; // Gọi DbContext của sếp ra
using THLTW_B2.Models;     // Gọi Model Product ra
using Microsoft.AspNetCore.Authorization;
namespace THLTW_B2.Controllers
{
    [Authorize]
    public class CuaHangController : Controller
    {
        
        private readonly ApplicationDbContext _context;

        // Tiêm (Inject) cái chìa khóa kho ApplicationDbContext vào đây
        public CuaHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Vào kho (Database), lôi bảng Products ra. 
            // Dùng Include để lấy luôn thông tin của bảng Categories (ví dụ: Đồ Uống)
            var products = await _context.Products
                                         .Include(p => p.Category)
                                         .ToListAsync();

            // Nhét danh sách sản phẩm (lon Coca) vào View để nó hiển thị lên màn hình
            return View(products);
        }
    }
}