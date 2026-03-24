// Đảm bảo namespace là THLTW_B2.Models
namespace THLTW_B2.Models
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public decimal TodayRevenue { get; set; }
        public int NewBookings { get; set; }
        public int PendingMatches { get; set; }
        public IEnumerable<Booking> RecentBookings { get; set; }
    }
}