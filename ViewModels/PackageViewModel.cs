using System.ComponentModel.DataAnnotations;

namespace SmartGymBooking.ViewModels
{
    public class PackageViewModel
    {
        public long PackageId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên gói.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(0, 100000000, ErrorMessage = "Giá gói không hợp lệ.")]
        public decimal Price { get; set; }

        [Range(1, 3650, ErrorMessage = "Số ngày phải lớn hơn 0.")]
        public int DurationDays { get; set; }

        public bool IncludesPT { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
