using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THLTW_B2.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên đội bóng là bắt buộc")]
        [Display(Name = "Tên đội bóng")]
        [StringLength(100)]
        public string TeamName { get; set; }

        [Display(Name = "Logo đội bóng")]
        public string? LogoUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập danh sách thành viên")]
        [Display(Name = "Danh sách thành viên")]
        public string MembersList { get; set; } // Ví dụ: "Nguyễn Văn A (Tiền đạo), Trần Văn B (Thủ môn)..."

        [Display(Name = "Giới thiệu / Khẩu hiệu")]
        [MaxLength(500)]
        public string? Description { get; set; }

        // Liên kết với tài khoản người đăng ký (Đội trưởng)
        public string CaptainId { get; set; }
        [ForeignKey("CaptainId")]
        public virtual ApplicationUser Captain { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [Display(Name = "Tổng số trận")]
        public int TotalMatches { get; set; } = 0;

        [Display(Name = "Thắng")]
        public int Wins { get; set; } = 0;

        [Display(Name = "Hòa")]
        public int Draws { get; set; } = 0;

        [Display(Name = "Thua")]
        public int Losses { get; set; } = 0;

        [Display(Name = "Bàn thắng")]
        public int GoalsFor { get; set; } = 0;

        [Display(Name = "Bàn thua")]
        public int GoalsAgainst { get; set; } = 0;

        [Display(Name = "Điểm Uy tín (Fairplay)")]
        public int FairPlayPoint { get; set; } = 100; // Khởi đầu ai cũng có 100 điểm, bùng kèo trừ điểm

        // Thuộc tính tính toán nhanh (Tỉ lệ thắng) - Không lưu vào DB
        [NotMapped]
        public double WinRate => TotalMatches == 0 ? 0 : Math.Round((double)Wins / TotalMatches * 100, 1);
    }

    // ViewModel dùng riêng cho Form đăng ký để nhận file ảnh Logo
    public class TeamRegistrationViewModel
    {
        [Required(ErrorMessage = "Tên đội bóng là bắt buộc")]
        public string TeamName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập danh sách thành viên (cách nhau bằng dấu phẩy)")]
        public string MembersList { get; set; }

        public string? Description { get; set; }

        // IFormFile để hứng file ảnh upload từ người dùng
        public IFormFile? LogoFile { get; set; }
    }
}