using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using THLTW_B2.Models;
using THLTW_B2.Helpers; // Gọi thư viện VnPay vừa tạo
using System.Threading.Tasks;
using System;

namespace THLTW_B2.Controllers
{
    [Authorize]
    public class VipController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        // Bổ sung IConfiguration để đọc appsettings.json
        public VipController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        // ========================================================
        // 1. TẠO URL THANH TOÁN VÀ CHUYỂN HƯỚNG SANG VNPAY
        // ========================================================
        [HttpPost]
        public IActionResult CreatePaymentUrl()
        {
            var vnpay = new VnPayLibrary();

            // Lấy cấu hình từ appsettings.json
            string vnp_Returnurl = _configuration["VnPay:ReturnUrl"];
            string vnp_Url = _configuration["VnPay:BaseUrl"];
            string vnp_TmnCode = _configuration["VnPay:TmnCode"];
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];

            // Mã đơn hàng tự sinh (Dùng thời gian + mã random)
            string orderId = DateTime.Now.Ticks.ToString();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            // Giá gói VIP 150.000đ (VNPay quy định phải nhân 100)
            vnpay.AddRequestData("vnp_Amount", (150000 * 100).ToString());

            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan nang cap VIP 30 ngay");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", orderId);

            // Tạo URL và chuyển hướng
            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Json(new { success = true, url = paymentUrl });
        }

        // ========================================================
        // 2. NHẬN KẾT QUẢ TRẢ VỀ TỪ VNPAY SAU KHI KHÁCH THANH TOÁN
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in Request.Query)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            string vnp_HashSecret = _configuration["VnPay:HashSecret"];
            string vnp_SecureHash = Request.Query["vnp_SecureHash"];

            // VNPay trả về mã "00" nghĩa là Giao dịch thành công
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");

            bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (isValidSignature && vnp_ResponseCode == "00")
            {
                // THANH TOÁN THÀNH CÔNG -> CẬP NHẬT VIP LÊN DATABASE
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    user.IsVip = true;
                    if (user.VipExpirationDate == null || user.VipExpirationDate < DateTime.Now)
                        user.VipExpirationDate = DateTime.Now.AddDays(30);
                    else
                        user.VipExpirationDate = user.VipExpirationDate.Value.AddDays(30);

                    await _userManager.UpdateAsync(user);
                    await _signInManager.RefreshSignInAsync(user);

                    TempData["VipSuccess"] = "🎉 KÍCH HOẠT VIP THÀNH CÔNG! Chào mừng bạn đến với đặc quyền Đế Vương.";
                }
            }
            else
            {
                TempData["VipError"] = "Thanh toán thất bại hoặc đã bị hủy. Vui lòng thử lại!";
            }

            return RedirectToAction("Index");
        }
    }
}