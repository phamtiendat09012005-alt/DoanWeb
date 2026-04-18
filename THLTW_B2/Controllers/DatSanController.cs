using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;
using THLTW_B2.Helpers; // Thư viện VNPay sếp vừa tạo
using Microsoft.Extensions.Configuration; // Thư viện cần thiết để đọc appsettings.json
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace THLTW_B2.Controllers
{
    [Authorize] // Bắt buộc khách phải đăng nhập mới được đặt sân
    public class DatSanController : Controller
    {
        private readonly ApplicationDbContext _context;
        // BƯỚC QUAN TRỌNG: Khai báo biến _configuration để đọc file cấu hình VNPay
        private readonly IConfiguration _configuration;

        // Bổ sung IConfiguration vào hàm khởi tạo (Constructor)
        public DatSanController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        // ==========================================
        // 1. API: Kiểm tra sân trống 
        // ==========================================
        [HttpGet]
        public IActionResult GetAvailablePitches(DateTime date, string timeSlot)
        {
            var allPitches = _context.SoccerFields.Where(s => s.IsActive).ToList();

            var bookedPitchIds = _context.Bookings
                .Where(b => b.BookingDate.Date == date.Date && b.TimeSlot == timeSlot && b.Status != "Đã hủy")
                .Select(b => b.SoccerFieldId)
                .ToList();

            var matchedPitchNames = _context.MatchRequests
                .Where(m => m.MatchDate.Date == date.Date &&
                            m.TimeSlot == timeSlot &&
                            (m.OpponentName != null || m.OpponentTeamName != null || m.Status != "Đang chờ đối thủ"))
                .Select(m => m.PitchName.Trim())
                .ToList();

            var result = allPitches.Select(p => new {
                id = p.FieldId,
                name = p.Name,
                price = p.PricePerHour,
                isBooked = bookedPitchIds.Contains(p.FieldId) || matchedPitchNames.Contains(p.Name.Trim())
            }).ToList();

            return Json(result);
        }

        // ==========================================
        // 2. API LƯU ĐƠN & GỌI VNPAY
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> TaoDonDatSan(string soccerFieldIds, DateTime date, string timeSlot, decimal price, string note)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);

            if (string.IsNullOrEmpty(soccerFieldIds))
                return Json(new { success = false, message = "Chưa chọn sân." });

            bool isVip = user != null && user.IsVip && user.VipExpirationDate > DateTime.Now;

            // Bảo vệ thêm ở lớp Backend: Cố tình đặt Giờ Vàng mà không phải VIP là chặn
            if (timeSlot.Contains("Giờ vàng") && !isVip)
            {
                return Json(new { success = false, message = "👑 LỖI: Bạn không có quyền đặt Giờ Vàng!" });
            }

            var idList = soccerFieldIds.Split(',').Select(int.Parse).ToList();
            var pitchNames = new List<string>();

            // Tạo mã giao dịch VNPay ngẫu nhiên theo Tick của hệ thống
            string paymentRef = DateTime.Now.Ticks.ToString();
            decimal pricePerPitch = price / idList.Count;
            decimal depositPerPitch = isVip ? 0 : pricePerPitch * 0.3m;

            // LƯU VÀO DATABASE
            foreach (var fieldId in idList)
            {
                var pitch = _context.SoccerFields.Find(fieldId);
                if (pitch == null) continue;

                pitchNames.Add(pitch.Name);

                var booking = new Booking
                {
                    UserId = userId,
                    SoccerFieldId = fieldId,
                    BookingDate = date,
                    TimeSlot = timeSlot,
                    TotalPrice = pricePerPitch,
                    DepositAmount = depositPerPitch,
                    Status = isVip ? "Đã giữ chỗ (VIP)" : "Chờ thanh toán", // Nếu không phải VIP thì chỉ là "Chờ"
                    Note = paymentRef, // Tạm thời nhét mã VNPay vào đây để tí nữa đối chiếu
                    CreatedAt = DateTime.Now
                };
                _context.Bookings.Add(booking);

                // Cá lớn nuốt cá bé
                var pendingMatches = _context.MatchRequests.Where(m =>
                    m.MatchDate.Date == date.Date &&
                    m.TimeSlot == timeSlot &&
                    m.OpponentName == null &&
                    m.PitchName == pitch.Name).ToList();

                foreach (var match in pendingMatches)
                {
                    match.Status = "Đã hủy (Sân có người bao trọn)";
                }
            }
            await _context.SaveChangesAsync();

            // RẼ NHÁNH: VIP THÌ XONG LUÔN, KHÁCH THƯỜNG THÌ BẮT TRẢ TIỀN VNPAY
            if (isVip)
            {
                // Gửi mail cho VIP
                string emailNhan = user?.Email ?? "";
                string tenNhan = user?.FullName ?? user?.UserName ?? "Khách hàng";
                if (!string.IsNullOrEmpty(emailNhan))
                {
                    _ = Task.Run(() => SendConfirmationEmail(emailNhan, tenNhan, date.ToString("dd/MM/yyyy"), timeSlot, pitchNames, price, true));
                }
                return Json(new { success = true, isVip = true });
            }
            else
            {
                // Tính tổng tiền cọc 30%
                decimal totalDeposit = price * 0.3m;

                // TẠO LINK VNPAY ĐỂ THU TIỀN CỌC
                var vnpay = new VnPayLibrary();
                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", _configuration["VnPay:TmnCode"]);
                // Nhân với 100 theo quy định của VNPay
                vnpay.AddRequestData("vnp_Amount", (Math.Round(totalDeposit) * 100).ToString());
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan tien coc san bong: " + paymentRef);
                vnpay.AddRequestData("vnp_OrderType", "other");

                // Móc đường dẫn ReturnUrlBooking từ appsettings.json
                vnpay.AddRequestData("vnp_ReturnUrl", _configuration["VnPay:ReturnUrlBooking"]);
                vnpay.AddRequestData("vnp_TxnRef", paymentRef);

                string paymentUrl = vnpay.CreateRequestUrl(_configuration["VnPay:BaseUrl"], _configuration["VnPay:HashSecret"]);

                // Trả về cho Front-end cái link VNPay
                return Json(new { success = true, isVip = false, url = paymentUrl });
            }
        }

        // ==========================================
        // 3. API NHẬN PHẢN HỒI TỪ VNPAY KHI KHÁCH ĐÃ QUẸT THẺ XONG
        // ==========================================
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

            string vnp_SecureHash = Request.Query["vnp_SecureHash"];
            string paymentRef = vnpay.GetResponseData("vnp_TxnRef"); // Cái mã Random Tick lúc nãy
            string responseCode = vnpay.GetResponseData("vnp_ResponseCode");

            if (vnpay.ValidateSignature(vnp_SecureHash, _configuration["VnPay:HashSecret"]) && responseCode == "00")
            {
                // THÀNH CÔNG: Tìm lại cái đơn đặt lúc nãy trong Database và đổi trạng thái
                var bookings = _context.Bookings.Where(b => b.Note == paymentRef).ToList();
                foreach (var b in bookings)
                {
                    b.Status = "Đã cọc";
                    b.Note = "Thanh toán qua VNPay thành công";
                }
                await _context.SaveChangesAsync();

                // Gửi thông báo thành công ra màn hình Web
                TempData["Message"] = "🎉 Thanh toán tiền cọc thành công qua VNPay! Sân đã được giữ cho bạn.";
            }
            else
            {
                // THẤT BẠI HOẶC BỊ HỦY: Xóa ngay đơn "Chờ thanh toán" đó đi để nhả sân cho người khác đặt
                var bookings = _context.Bookings.Where(b => b.Note == paymentRef).ToList();
                if (bookings.Any())
                {
                    _context.Bookings.RemoveRange(bookings);
                    await _context.SaveChangesAsync();
                }
                TempData["Error"] = "⚠️ Giao dịch bị hủy hoặc thất bại! Hệ thống đã giải phóng lịch sân vừa đặt.";
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. API ĐỔ DỮ LIỆU LỊCH RA FULLCALENDAR
        // ==========================================
        [HttpGet]
        public IActionResult GetCalendarEvents()
        {
            var events = new List<object>();

            var bookings = _context.Bookings.Where(b => b.Status != "Đã hủy" && b.TimeSlot.Contains("-")).ToList();
            foreach (var b in bookings)
            {
                var pitch = _context.SoccerFields.Find(b.SoccerFieldId);
                if (pitch == null) continue;

                // FIX: Cắt bỏ chữ "(Giờ vàng)" để lịch đọc được
                string startTime = b.TimeSlot.Split("-")[0].Trim();
                string endTime = b.TimeSlot.Split("-")[1].Replace("(Giờ vàng)", "").Replace("(Sáng sớm)", "").Trim();

                events.Add(new
                {
                    title = pitch.Name + " (Đã đặt)",
                    start = b.BookingDate.ToString("yyyy-MM-dd") + "T" + startTime,
                    end = b.BookingDate.ToString("yyyy-MM-dd") + "T" + endTime,
                    color = "#dc3545",
                    textColor = "white"
                });
            }

            var matches = _context.MatchRequests.Where(m => m.OpponentName != null && m.TimeSlot.Contains("-")).ToList();
            foreach (var m in matches)
            {
                string startTime = m.TimeSlot.Split("-")[0].Trim();
                string endTime = m.TimeSlot.Split("-")[1].Replace("(Giờ vàng)", "").Replace("(Sáng sớm)", "").Trim();

                events.Add(new
                {
                    title = m.PitchName + " (Cáp kèo)",
                    start = m.MatchDate.ToString("yyyy-MM-dd") + "T" + startTime,
                    end = m.MatchDate.ToString("yyyy-MM-dd") + "T" + endTime,
                    color = "#fd7e14",
                    textColor = "white"
                });
            }

            return Json(events);
        }

        // ==========================================
        // 5. HÀM GỬI EMAIL CHỈ DÀNH CHO VIP (VÌ KHÁCH THƯỜNG PAY QUA VNPAY SẼ CÓ BILL VNPAY RỒI)
        // ==========================================
        private void SendConfirmationEmail(string toEmail, string fullName, string date, string time, List<string> pitchNames, decimal totalPrice, bool isVip)
        {
            try
            {
                // Thay bằng Mail của sếp
                string fromEmailAddress = "phamtiendat09012005@gmail.com";
                string appPassword = "vmxx zgir hztr szhj";

                var fromAddress = new System.Net.Mail.MailAddress(fromEmailAddress, "Hệ Thống Sân Bóng VIP");
                var toAddress = new System.Net.Mail.MailAddress(toEmail);

                string subject = $"[ĐẶC QUYỀN VIP] - Xác nhận đặt sân ngày {date}";
                string paymentInfo = "<tr><td style='padding: 8px; border: 1px solid #ddd;'><b>Thanh toán:</b></td><td style='padding: 8px; border: 1px solid #ddd; color: #d63384;'><b>👑 MIỄN CỌC. THANH TOÁN 100% TẠI SÂN.</b></td></tr>";

                string body = $@"
                    <div style='font-family: Arial; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                        <h2 style='color: #0d6efd;'>XÁC NHẬN ĐẶT SÂN THÀNH CÔNG</h2>
                        <p>Chào <b>{fullName}</b>,</p>
                        <p>Bạn đã sử dụng đặc quyền VIP để giữ sân mà không cần trả trước. Thông tin chi tiết:</p>
                        <table style='width: 100%; border-collapse: collapse; margin: 15px 0;'>
                            <tr><td style='padding: 8px; border: 1px solid #ddd;'><b>Ngày thi đấu:</b></td><td style='padding: 8px; border: 1px solid #ddd;'>{date}</td></tr>
                            <tr><td style='padding: 8px; border: 1px solid #ddd;'><b>Khung giờ:</b></td><td style='padding: 8px; border: 1px solid #ddd; color: red;'><b>{time}</b></td></tr>
                            <tr><td style='padding: 8px; border: 1px solid #ddd;'><b>Tổng tiền sân:</b></td><td style='padding: 8px; border: 1px solid #ddd;'>{totalPrice:N0} VNĐ</td></tr>
                            {paymentInfo}
                        </table>
                        <p>Vui lòng đến sớm 10 phút. Cảm ơn bạn!</p>
                    </div>";

                var smtp = new System.Net.Mail.SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(fromAddress.Address, appPassword)
                };

                using (var message = new System.Net.Mail.MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    System.Diagnostics.Debug.WriteLine($"[MAIL LOG] Đang thử gửi mail tới: {toEmail}...");
                    smtp.Send(message);
                    System.Diagnostics.Debug.WriteLine($"[MAIL LOG] GỬI MAIL THÀNH CÔNG!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI CODE GỬI MAIL]: {ex.Message}");
            }
        }
    }
}