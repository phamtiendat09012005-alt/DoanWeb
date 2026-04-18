using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace THLTW_B2.Areas.Employee.Controllers
{
    [Area("Employee")]
    public class LichSanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LichSanController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View("~/Areas/Employee/Views/Home/LichSan.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> GetCalendarEvents()
        {
            var eventList = new List<object>();

            // 1. XỬ LÝ LỊCH ĐẶT SÂN
            var bookings = await _context.Bookings
                .Include(b => b.SoccerField)
                .Include(b => b.User) // Cần Include User để lấy tên
                .Where(b => b.Status != "Đã hủy")
                .ToListAsync();

            foreach (var b in bookings)
            {
                if (string.IsNullOrEmpty(b.TimeSlot) || !b.TimeSlot.Contains("-")) continue;

                var times = b.TimeSlot.Split('-');
                string startTime = times[0].Trim();

                // Gọt sạch chữ (nếu có), chỉ lấy 5 ký tự đầu "HH:mm"
                string endTimeRaw = times[1].Trim();
                string endTime = endTimeRaw.Length > 5 ? endTimeRaw.Substring(0, 5).Trim() : endTimeRaw;

                try
                {
                    eventList.Add(new
                    {
                        id = "b_" + b.Id,
                        title = (b.SoccerField?.Name ?? "Sân") + " (Đã đặt)",
                        start = b.BookingDate.ToString("yyyy-MM-dd") + "T" + startTime + ":00",
                        end = b.BookingDate.ToString("yyyy-MM-dd") + "T" + endTime + ":00",
                        color = "#d9534f", // Đỏ đô
                        extendedProps = new { info = "Khách: " + (b.User?.FullName ?? b.User?.UserName ?? "Khách lẻ") }
                    });
                }
                catch { /* Bỏ qua nếu có lỗi format ở 1 đơn lẻ */ }
            }

            // 2. XỬ LÝ KÈO TÌM ĐỐI THỦ
            var matches = await _context.MatchRequests.ToListAsync();
            foreach (var m in matches)
            {
                if (string.IsNullOrEmpty(m.TimeSlot) || !m.TimeSlot.Contains("-")) continue;

                var times = m.TimeSlot.Split('-');
                string startTime = times[0].Trim();

                string endTimeRaw = times[1].Trim();
                string endTime = endTimeRaw.Length > 5 ? endTimeRaw.Substring(0, 5).Trim() : endTimeRaw;

                try
                {
                    eventList.Add(new
                    {
                        id = "m_" + m.Id,
                        title = "🔥 Kèo: " + (m.PitchName ?? "Sân"),
                        start = m.MatchDate.ToString("yyyy-MM-dd") + "T" + startTime + ":00",
                        end = m.MatchDate.ToString("yyyy-MM-dd") + "T" + endTime + ":00",
                        color = "#f0ad4e", // Cam
                        extendedProps = new { info = "Đội: " + m.TeamName }
                    });
                }
                catch { }
            }

            return Json(eventList);
        }
    }
}