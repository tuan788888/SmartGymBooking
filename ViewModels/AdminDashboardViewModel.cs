using SmartGymBooking.Models;

namespace SmartGymBooking.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Summary cards
        public int TotalCustomers { get; set; }

        public int ActiveCustomers { get; set; }

        public int TotalPTs { get; set; }

        public int ActivePTs { get; set; }

        public int ActiveMemberships { get; set; }

        public int TodayBookings { get; set; }

        public int TodayCheckIns { get; set; }

        public int PendingPayments { get; set; }

        public decimal RevenueThisMonth { get; set; }

        public int UpcomingPTSchedules { get; set; }


        // Revenue chart
        public List<string> MonthlyRevenueLabels { get; set; }
            = new();

        public List<decimal> MonthlyRevenueValues { get; set; }
            = new();


        // Booking status chart
        public int PendingBookings { get; set; }

        public int ConfirmedBookings { get; set; }

        public int CompletedBookings { get; set; }

        public int CancelledBookings { get; set; }


        // Recent data
        public List<Payment> RecentPayments { get; set; }
            = new();

        public List<Booking> RecentBookings { get; set; }
            = new();

        public List<Ptschedule> UpcomingSchedules { get; set; }
            = new();
    }
}
