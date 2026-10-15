using System.ComponentModel.DataAnnotations;

namespace SmartGymBooking.ViewModels;

public sealed class PastDateAttribute : ValidationAttribute
{
    public PastDateAttribute() => ErrorMessage = "Ngày sinh phải trước ngày hiện tại.";

    public override bool IsValid(object? value) => value == null ||
        value is DateOnly date && date < DateOnly.FromDateTime(DateTime.Today);
}
