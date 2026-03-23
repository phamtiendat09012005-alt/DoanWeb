using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THLTW_B2.Models
{
    public class EmployeeShift
    {
        [Key]
        public int Id { get; set; }

        // Liên kết với bảng Nhân viên (Tài khoản Identity)
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        // Ngày làm việc
        public DateTime WorkDate { get; set; }

        // Tên ca (VD: Ca sáng (06:00 - 12:00), Ca chiều, Ca tối)
        public string ShiftName { get; set; }

        // Mức lương cho ca làm việc này (VD: 150000)
        public decimal ShiftWage { get; set; }

        // Xác nhận nhân viên có đi làm hay không (Điểm danh)
        public bool IsAttended { get; set; } = true;

        // Trạng thái: Đã thanh toán lương cho ca này chưa?
        public bool IsPaid { get; set; } = false;
    }
}