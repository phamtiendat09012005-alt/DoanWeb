using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THLTW_B2.Models
{
    public class OrderDetail
    {
        [Key]
        public int OrderDetailId { get; set; }

        // Khóa ngoại liên kết với Hóa Đơn
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        // Khóa ngoại liên kết với Sản Phẩm
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }

        [Display(Name = "Đơn giá lúc mua")]
        public decimal Price { get; set; }
    }
}