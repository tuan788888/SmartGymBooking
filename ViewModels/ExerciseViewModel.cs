using System.ComponentModel.DataAnnotations;

namespace SmartGymBooking.ViewModels
{
    public class ExerciseViewModel
    {
        public long ExerciseId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên bài tập.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? MuscleGroup { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn độ khó.")]
        [RegularExpression("^(BEGINNER|INTERMEDIATE|ADVANCED)$", ErrorMessage = "Độ khó không hợp lệ.")]
        public string Difficulty { get; set; } = "BEGINNER";

        [StringLength(1000)]
        public string? Instructions { get; set; }

        [Url(ErrorMessage = "Link video không hợp lệ.")]
        [RegularExpression("^https?://.+$", ErrorMessage = "Link video phải dùng HTTP hoặc HTTPS.")]
        [StringLength(1000)]
        public string? VideoUrl { get; set; }
    }
}
