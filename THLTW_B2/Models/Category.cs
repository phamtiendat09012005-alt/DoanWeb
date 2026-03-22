using System.ComponentModel.DataAnnotations;

namespace THLTW_B2.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        // Mối quan hệ 1-N: Một danh mục có nhiều sản phẩm
        public virtual ICollection<Product> Products { get; set; }
    }
}
