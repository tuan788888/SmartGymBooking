using System.ComponentModel.DataAnnotations;
using SmartGymBooking.Services;

namespace SmartGymBooking.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(255, ErrorMessage = "Email không được dài quá 255 ký tự.")]
    [RegularExpression(@"[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*@[Gg][Mm][Aa][Ii][Ll]\.[Cc][Oo][Mm]", ErrorMessage = "Vui lòng nhập địa chỉ có đuôi @gmail.com hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"[0-9]{1,11}", ErrorMessage = "Số điện thoại chỉ được chứa tối đa 11 chữ số.")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập ngày sinh.")]
    [DataType(DataType.Date)]
    [MinimumCustomerAge]
    public DateOnly? DateOfBirth { get; set; }

    [RegularExpression("^(MALE|FEMALE|OTHER)$", ErrorMessage = "Giới tính không hợp lệ.")]
    public string? Gender { get; set; }
}

public sealed class PastDateAttribute : ValidationAttribute
{
    public PastDateAttribute() => ErrorMessage = "Ngày sinh phải trước ngày hiện tại.";

    public override bool IsValid(object? value) => value == null ||
        value is DateOnly date && date < DateOnly.FromDateTime(DateTime.Today);
}

public sealed class MinimumCustomerAgeAttribute : ValidationAttribute
{
    public MinimumCustomerAgeAttribute() => ErrorMessage = "Khách hàng phải đủ 16 tuổi.";

    public override bool IsValid(object? value) => value == null ||
        value is DateOnly date && date <= DateOnly.FromDateTime(GymTime.Now).AddYears(-16);
}
