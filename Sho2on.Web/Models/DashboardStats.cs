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
}