using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartGymBooking.ViewModels
{
    public class BookingCreateViewModel
    {
        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn dịch vụ.")]
        public long ServiceId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày tập.")]
        public DateOnly? BookingDate { get; set; }

        public List<SelectListItem> Services { get; set; }
            = new();
    }
}
