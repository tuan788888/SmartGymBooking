using SmartGymBooking.Models;

namespace SmartGymBooking.ViewModels
{
    public class EmployeeDashboardViewModel
    {
        public int TodayBookings { get; set; }

        public int PendingBookings { get; set; }

        public int TodayCheckIns { get; set; }

        public int PendingPayments { get; set; }

        public int TodayPTSchedules { get; set; }

        public List<Booking> UpcomingBookings { get; set; }
            = new();

        public List<Payment> PendingPaymentList { get; set; }
            = new();
    }
}
