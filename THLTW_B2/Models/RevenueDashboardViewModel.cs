using System.Collections.Generic;

namespace THLTW_B2.Models
{
    public class RevenueDashboardViewModel
    {
        public decimal TotalRevenue { get; set; } // Tổng thu
        public decimal TotalExpense { get; set; } // Tổng chi
        public decimal NetProfit => TotalRevenue - TotalExpense; // Lợi nhuận ròng

        // Danh sách hiển thị
        public List<BookingHistoryViewModel> RecentRevenues { get; set; }
        public List<Expense> RecentExpenses { get; set; }
    }
}