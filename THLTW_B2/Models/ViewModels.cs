namespace THLTW_B2.Models
{
    public class UserListViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public IList<string> Roles { get; set; }
    }

    // Class dùng cho trang EditRoles
    public class EditRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string RoleName { get; set; }
    }

    // Class dùng cho trang Create
    public class CreateStaffViewModel
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public string SelectedRole { get; set; }
    }

    public class BookingHistoryViewModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string TimeSlot { get; set; }
        public string PitchName { get; set; }
        public string CustomerName { get; set; } // Tên người đặt hoặc Đội chủ nhà
        public string? OpponentName { get; set; } // Chỉ dành cho kèo ghép
        public string Type { get; set; } // "Đặt sân lẻ" hoặc "Kèo ghép"
        public string Status { get; set; }
        public bool IsMatch { get; set; } // Phân loại để hiển thị icon
    }
}
