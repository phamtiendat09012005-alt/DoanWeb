using System;
using System.ComponentModel.DataAnnotations;

namespace THLTW_B2.Models
{
    public class Expense
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khoản chi")]
        public string Title { get; set; } // VD: Trả lương tháng 3, Tiền điện...

        [Required]
        public string Category { get; set; } // Phân loại: Lương, Nhập hàng, Bảo trì, Khác

        [Required]
        public decimal Amount { get; set; } // Số tiền chi ra

        public DateTime ExpenseDate { get; set; } = DateTime.Now; // Ngày chi

        public string? Note { get; set; } // Ghi chú thêm (người nhận, mã hóa đơn...)
    }
}