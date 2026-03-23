using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THLTW_B2.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public string InvoiceCode { get; set; } // Mã hóa đơn (HD-..., POS-...)

        public int? BookingId { get; set; } // Nếu là tiền sân thì nối với đơn đặt sân
        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        public decimal Amount { get; set; } // Số tiền

        public string PaymentMethod { get; set; } // Tiền mặt / Chuyển khoản

        public string Status { get; set; } // Thành công / Chờ xử lý

        public DateTime PaymentDate { get; set; }

        public string? Note { get; set; } // Ghi chú (Bán nước, thu tiền sân...)
    }
}