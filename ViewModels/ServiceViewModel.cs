using System.ComponentModel.DataAnnotations;

namespace SmartGymBooking.ViewModels
{
    public class ServiceViewModel
    {
        public long ServiceId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
