using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartGymBooking.ViewModels
{
    public class BookingCreateViewModel
    {
        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn dịch vụ.")]
        public long ServiceId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
        public DateTime EndTime { get; set; }

        public List<SelectListItem> Services { get; set; }
            = new();
    }
}
