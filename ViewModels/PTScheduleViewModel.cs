using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartGymBooking.ViewModels
{
    public class PTScheduleViewModel
    {
        public long PTScheduleId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn PT.")]
        [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn PT.")]
        public long PTId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khách hàng.")]
        [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn khách hàng.")]
        public long CustomerId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
        public DateTime EndTime { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public List<SelectListItem> PTs { get; set; }
            = new();

        public List<SelectListItem> Customers { get; set; }
            = new();
    }
}
