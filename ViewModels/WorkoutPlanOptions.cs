using System.ComponentModel.DataAnnotations;

namespace SmartGymBooking.ViewModels;

public sealed class WorkoutPlanOptions
{
    [Required(ErrorMessage = "Hãy chọn mục tiêu.")]
    [RegularExpression("HYPERTROPHY|WEIGHT_LOSS|FITNESS", ErrorMessage = "Mục tiêu không hợp lệ.")]
    public string? Goal { get; set; }

    [Required(ErrorMessage = "Hãy chọn trình độ.")]
    [RegularExpression("BEGINNER|INTERMEDIATE|ADVANCED", ErrorMessage = "Trình độ không hợp lệ.")]
    public string? Level { get; set; }

    [Required(ErrorMessage = "Hãy chọn số buổi mỗi tuần.")]
    [Range(3, 5, ErrorMessage = "Hãy chọn từ 3 đến 5 buổi mỗi tuần.")]
    public int? SessionsPerWeek { get; set; }
}
