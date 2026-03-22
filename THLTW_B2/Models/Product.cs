using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace THLTW_B2.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm (Vd: Bò húc, Nước suối)")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Giá bán (VNĐ)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho")]
        [Range(0, 10000, ErrorMessage = "Số lượng tồn không hợp lệ")]
        [Display(Name = "Số lượng tồn kho")]
        public int StockQuantity { get; set; }

        [Display(Name = "Đơn vị tính (Vd: Chai, Lon, Cái)")]
        public string Unit { get; set; }

        // Khóa ngoại liên kết với bảng Category
        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }
        [Display(Name = "Đường dẫn ảnh")]
        public string? ImageUrl { get; set; }
        [NotMapped] 
        [Display(Name = "Tải ảnh lên")]
        public IFormFile? ImageUpload { get; set; }

    }

}
