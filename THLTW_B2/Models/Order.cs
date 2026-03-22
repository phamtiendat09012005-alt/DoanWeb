using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace THLTW_B2.Models // Sếp nhớ check lại namespace cho khớp project nhé
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        // Lưu ID của user đang đăng nhập (Để biết ai mua)
        public string? UserId { get; set; }

        [Display(Name = "Ngày đặt")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Tên người nhận không được để trống")]
        [Display(Name = "Tên người nhận")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Tổng tiền")]
        public decimal TotalAmount { get; set; }

        // Trạng thái: 0 = Chờ xử lý, 1 = Đã thanh toán, 2 = Đã hủy
        public int Status { get; set; } = 0;

        // Quan hệ 1-N: 1 Hóa đơn có nhiều Chi tiết
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}