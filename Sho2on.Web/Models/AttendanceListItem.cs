namespace Sho2on.Web.Models
{
    public class AttendanceListItem
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public string BranchName { get; set; } = "";
        public DateTime AttendanceDate { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public TimeSpan? Late { get; set; }
        public string Status { get; set; } = ""; // حاضر / غائب / إجازة / عطلة / لم يسجل بعد
    }
}