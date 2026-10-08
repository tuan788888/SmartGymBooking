using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartGymBooking.ViewModels
{
    public class PTViewModel
    {
        public long PTId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên PT.")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(255)]
        public string? Email { get; set; }

        [StringLength(150)]
        public string? Specialization { get; set; }

        [Range(0, 60, ErrorMessage = "Số năm kinh nghiệm không hợp lệ.")]
        public int? Experience { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public string? CurrentAvatar { get; set; }

        public IFormFile? AvatarFile { get; set; }

        [Required]
        [RegularExpression("^(ACTIVE|INACTIVE)$", ErrorMessage = "Trạng thái không hợp lệ.")]
        public string Status { get; set; } = "ACTIVE";
    }
}
