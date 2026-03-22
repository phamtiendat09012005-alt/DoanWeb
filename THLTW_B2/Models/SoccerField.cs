using System.ComponentModel.DataAnnotations;

namespace THLTW_B2.Models
{
    public class SoccerField
    {
        [Key]
        public int FieldId { get; set; }

        [Required(ErrorMessage = "Tên sân không được để trống")]
        [Display(Name = "Tên sân bóng")]
        public string Name { get; set; } // VD: Sân 5 - A1

        [Required(ErrorMessage = "Vui lòng chọn loại sân")]
        [Display(Name = "Loại sân")]
        public string Type { get; set; } // "5 người" hoặc "7 người"

        [Required(ErrorMessage = "Giá thuê là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá thuê (VND/Giờ)")]
        public decimal PricePerHour { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true; // Sân có sẵn hay đang bảo trì

        [Display(Name = "Mô tả thêm")]
        public string? Description { get; set; }
        [Display(Name = "Hình ảnh sân")]
        public string? ImageUrl { get; set; }
    }
}