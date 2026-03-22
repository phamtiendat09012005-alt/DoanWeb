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
}
