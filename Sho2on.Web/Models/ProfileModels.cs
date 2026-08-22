namespace Sho2on.Web.Models
{
    public class MyProfileInfo
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Code { get; set; } = "";
        public string Username { get; set; } = "";
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string JobTitleName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string BranchName { get; set; } = "";
        public string? ManagerName { get; set; }
        public DateOnly HireDate { get; set; }
        public DateOnly BirthDate { get; set; }
        public int LeaveBalance { get; set; }
        public int LeaveUsed { get; set; }
    }

    public class MyRequestItem
    {
        public string Type { get; set; } = "";       // إجازة / إذن / مأمورية / سلفة / نقل
        public string Icon { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = "";
        public string StatusClass { get; set; } = "";
    }
}