using System.ComponentModel.DataAnnotations;

namespace THLTW_B2.Models
{
    public class MatchRequest
    {
        [Key]
        public int Id { get; set; }
        public string TeamName { get; set; }
        public DateTime MatchDate { get; set; }
        public string TimeSlot { get; set; }
        public string Phone { get; set; }
        public string PitchName { get; set; }
        public string Note { get; set; }
        public bool IsDeposited { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? OpponentTeamName { get; set; } // Thêm trường này (cho phép null vì lúc đầu chưa có đối thủ)
        public bool IsAccepted => !string.IsNullOrEmpty(OpponentTeamName);
        public string Status { get; set; } = "Đang chờ đối thủ"; //
        public string? OpponentName { get; set; }
        public string? OpponentPhone { get; set; }
        public bool IsOpponentDeposited { get; set; } = false;
        // Tỉ số của đội chủ nhà (Người tạo kèo)
        public int? HostScore { get; set; }

        // Tỉ số của đội khách (Người nhận kèo)
        public int? OpponentScore { get; set; }

        // Xác nhận trận đấu đã đá xong và chốt kết quả
        public bool IsCompleted { get; set; } = false;

    }
}
