using System.ComponentModel.DataAnnotations;

namespace SmartGymBooking.ViewModels;

public class CustomerProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150, ErrorMessage = "Họ tên không được dài quá 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;
}
