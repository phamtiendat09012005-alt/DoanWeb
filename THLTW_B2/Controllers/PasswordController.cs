using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using THLTW_B2.Models;

namespace THLTW_B2.Controllers
{
    public class PasswordController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public PasswordController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // 1. Giao diện trang nhập Email
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // 2. Nút Bấm "Gửi Mã"
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập Email!";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "Email không tồn tại trong hệ thống!";
                return View();
            }

            // TẠO MÃ 6 SỐ NGẪU NHIÊN VÀ LƯU VÀO DATABASE
            var otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpiryTime = DateTime.Now.AddMinutes(5); // Mã sống được 5 phút
            await _userManager.UpdateAsync(user);

            // GỬI EMAIL CHỨA MÃ OTP
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    // THAY EMAIL VÀ MẬT KHẨU 16 CHỮ CÁI (KHÔNG KHOẢNG TRẮNG) CỦA BẠN VÀO ĐÂY:
                    Credentials = new NetworkCredential("huyho150705@gmail.com", "gxrifxjcmukgzmpr"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    // THAY LẠI EMAIL CỦA BẠN VÀO ĐÂY 1 LẦN NỮA:
                    From = new MailAddress("huyho150705@gmail.com", "Hệ thống QLSBMini"),
                    Subject = "Mã xác nhận lấy lại mật khẩu",
                    Body = $"<h3>Mã OTP của bạn là: <strong style='color:blue; font-size:24px'>{otp}</strong></h3><p>Mã này có hiệu lực trong 5 phút. Vui lòng không chia sẻ cho ai.</p>",
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(email); // Gửi đến cái email mà khách vừa nhập
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống gửi mail: " + ex.Message;
                return View();
            }

            // Nếu gửi thành công, tạm thời trả về một trang thông báo thành công (Bước sau mình sẽ làm trang Nhập mã 6 số)
            return RedirectToAction("ResetPassword", new { email = email });
        }
        // 3. Hiển thị trang nhập OTP và Mật khẩu mới
        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");
            ViewBag.Email = email; // Gửi email ra ngoài giao diện
            return View();
        }

        // 4. Xử lý khi bấm nút "Đổi mật khẩu"
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string otp, string newPassword, string confirmPassword)
        {
            ViewBag.Email = email;

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("ForgotPassword");

            // KIỂM TRA MÃ OTP
            if (user.OtpCode != otp)
            {
                ViewBag.Error = "Mã OTP không chính xác!";
                return View();
            }

            // KIỂM TRA HẾT HẠN (5 PHÚT)
            if (user.OtpExpiryTime < DateTime.Now)
            {
                ViewBag.Error = "Mã OTP đã hết hạn! Vui lòng gửi lại mã mới.";
                return View();
            }

            // TIẾN HÀNH ĐỔI MẬT KHẨU BẰNG LỆNH CỦA IDENTITY
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (result.Succeeded)
            {
                // Đổi pass xong thì xóa mã OTP đi cho an toàn
                user.OtpCode = null;
                user.OtpExpiryTime = null;
                await _userManager.UpdateAsync(user);

                // Chuyển hướng về trang Đăng nhập
                return Redirect("/Identity/Account/Login");
            }

            ViewBag.Error = "Có lỗi xảy ra khi đổi mật khẩu. Mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt!";
            return View();
        }
    }
}