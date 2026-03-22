using Microsoft.AspNetCore.SignalR;
using THLTW_B2.DataAccess;
using THLTW_B2.Models;

namespace THLTW_B2.Hubs
{
    // Kế thừa từ class Hub của SignalR
    public class MatchHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public MatchHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AcceptMatch(int matchId, string posterName, string acceptorName, string acceptorPhone)
        {
            var match = await _context.MatchRequests.FindAsync(matchId);

            if (match != null)
            {
                // 1. Cập nhật vào Database
                match.Status = "Đã có đối thủ"; // Bổ sung: Đổi trạng thái kèo
                match.OpponentTeamName = acceptorName;
                match.OpponentPhone = acceptorPhone;
                match.IsOpponentDeposited = true; // Bổ sung: Xác nhận người nhận kèo đã thanh toán cọc

                await _context.SaveChangesAsync();

                // 2. Phát tín hiệu Real-time cho mọi người để cập nhật giao diện
                // Gửi ID để các trình duyệt tìm đúng thẻ kèo để sửa
                await Clients.All.SendAsync("MatchAcceptedUpdate", posterName, acceptorName, acceptorPhone);
            }
        }

        public async Task FindOpponent(string user, string date, string time, string phone, string pitch, string note)
        {
            // 1. Lưu vào Database
            var newMatch = new MatchRequest
            {
                TeamName = user,
                MatchDate = DateTime.Parse(date),
                TimeSlot = time,
                Phone = phone,
                PitchName = pitch,
                Note = note,
                IsDeposited = true // Giả định đã thanh toán cọc thành công khi tạo kèo
            };

            _context.MatchRequests.Add(newMatch);
            await _context.SaveChangesAsync();

            // 2. Định dạng lại tin nhắn để gửi Real-time (giống giao diện cũ của bạn)
            string fullMsg = $@"
            <div class='d-flex justify-content-between mb-2'>
                <span class='text-light fw-bold small'><i class='fa-regular fa-calendar me-1'></i>{date}</span>
                <span class='text-warning fw-bold small'><i class='fa-regular fa-clock me-1'></i>{time}</span>
            </div>
            <div class='text-white mb-2 small'><i class='fa-solid fa-location-dot me-2 text-info'></i>Sân: {pitch}</div>
            <div class='text-white small mb-2'>
                <i class='fa-solid fa-phone me-2 text-secondary'></i>{phone} | {note}
            </div>
            <div class='mt-2'><span class='badge bg-success small'>ĐÃ CỌC SÂN</span></div>";

            // 3. Broadcast cho mọi người
            await Clients.All.SendAsync("ReceiveOpponentRequest", user, fullMsg);
        }
    }
}