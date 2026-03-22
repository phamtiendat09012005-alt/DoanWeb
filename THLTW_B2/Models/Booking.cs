using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THLTW_B2.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        // 1. Ai là người đặt?
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        // 2. Đặt sân nào?
        [Required]
        public int SoccerFieldId { get; set; }
        [ForeignKey("SoccerFieldId")]
        public SoccerField SoccerField { get; set; }

        // 3. Đặt ngày giờ nào?
        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        public string TimeSlot { get; set; }

        // 4. Tiền nong & Trạng thái
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }

        public string Status { get; set; } = "Đã cọc"; // Đã cọc, Đã hủy, Đã hoàn thành

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}