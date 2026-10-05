using System.ComponentModel.DataAnnotations;

namespace SmartGymBooking.ViewModels
{
    public class EmployeeViewModel
    {
        public long EmployeeId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        [RegularExpression("^(MALE|FEMALE|OTHER)$", ErrorMessage = "Giới tính không hợp lệ.")]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chức vụ.")]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Avatar { get; set; }

        public bool IsActive { get; set; } = true;

        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}
