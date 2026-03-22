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
        public string? OpponentTeamName { get; set; }
        public bool IsAccepted => !string.IsNullOrEmpty(OpponentTeamName);
        public string Status { get; set; } = "Đang chờ đối thủ";
        public string? OpponentName { get; set; }
        public string? OpponentPhone { get; set; }
        public bool IsOpponentDeposited { get; set; } = false;

        // Tỉ số
        public int? HostScore { get; set; }
        public int? OpponentScore { get; set; }
        public bool IsCompleted { get; set; } = false;

        // BỔ SUNG: TRƯỜNG LƯU SỐ TIỀN CỌC ĐÃ BỊ THIẾU
        public decimal TienCoc { get; set; }
    }
}