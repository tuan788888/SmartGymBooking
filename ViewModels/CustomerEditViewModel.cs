using System.ComponentModel.DataAnnotations;

namespace SmartGymBooking.ViewModels
{
    public class CustomerEditViewModel
    {
        public long CustomerId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        [RegularExpression("^(MALE|FEMALE|OTHER)$", ErrorMessage = "Giới tính không hợp lệ.")]
        public string? Gender { get; set; }

        [Range(50, 250, ErrorMessage = "Chiều cao không hợp lệ.")]
        public decimal? Height { get; set; }

        [Range(20, 300, ErrorMessage = "Cân nặng không hợp lệ.")]
        public decimal? Weight { get; set; }

        [RegularExpression("^(WEIGHT_LOSS|MUSCLE_GAIN|MAINTAIN)$", ErrorMessage = "Mục tiêu không hợp lệ.")]
        public string? FitnessGoal { get; set; }

        [RegularExpression("^(BEGINNER|INTERMEDIATE|ADVANCED)$", ErrorMessage = "Trình độ không hợp lệ.")]
        public string? ExperienceLevel { get; set; }

        [RegularExpression("^(LOW|MODERATE|HIGH)$", ErrorMessage = "Mức vận động không hợp lệ.")]
        public string? ActivityLevel { get; set; }

        [Range(1, 7)]
        public int? PreferredSessionsPerWeek { get; set; }

        [Range(15, 180)]
        public int? PreferredSessionMinutes { get; set; }
    }
}
