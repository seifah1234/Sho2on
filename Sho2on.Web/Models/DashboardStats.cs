namespace Sho2on.Web.Models
{
    public class DashboardStats
    {
        public int TotalEmployees { get; set; }
        public int TodayAttendance { get; set; }
        public int TodayAbsence { get; set; }
        public int PendingLeaves { get; set; }
        public string? UserName { get; set; }
        public string? UserJob { get; set; }
        public string? UserDepartment { get; set; }
        public int LeaveBalance { get; set; }
    }

    public class ManagerDashboardStats
{
    public int TotalEmployees { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int LateToday { get; set; }
    public int PendingApprovals { get; set; }
}

public class TeamCheckInItem
{
    public string EmployeeName { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public DateTime? CheckInTime { get; set; }
    public bool IsLate { get; set; }
}
}