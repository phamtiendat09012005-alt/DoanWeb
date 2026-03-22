using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace THLTW_B2.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Vui lòng nhập Họ và Tên")]
        [MaxLength(100)]
        public string FullName { get; set; }
        public string? OtpCode { get; set; }           // Lưu mã 6 số
        public DateTime? OtpExpiryTime { get; set; }
    }
}
