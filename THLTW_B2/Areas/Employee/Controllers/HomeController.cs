using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace THLTW_B2.Areas.Employee.Controllers
{
    [Area("Employee")] // RẤT QUAN TRỌNG: Báo cho hệ thống biết Controller này thuộc khu vực Admin
    [Authorize(Roles = "Employee")] // BẢO MẬT: Chỉ có tài khoản mang quyền Admin mới được vào đây
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
