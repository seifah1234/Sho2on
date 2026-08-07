 namespace Sho2on.Web.Models
{
    public class MonthlyAttendanceDay
    {
        public int? AttendanceId { get; set; }
        public DateOnly Date { get; set; }
        public string DayName { get; set; } = "";

        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }

        public int? ShiftId { get; set; }
        public string? ShiftName { get; set; }

        public TimeSpan? Late { get; set; }
        public TimeSpan? EarlyLeave { get; set; }
        public TimeSpan? EarlyEnter { get; set; }
        public TimeSpan? Overtime { get; set; }
        public TimeSpan? TotalWorkHours { get; set; }
        public TimeSpan? ActualWorkHours { get; set; }
        public bool IsAbsence { get; set; }
        public bool IsHoliday { get; set; }
        public bool IsWeeklyRest { get; set; }
        public bool HasLeave { get; set; }

        public bool ExemptLate { get; set; }
        public bool ExemptEarlyLeave { get; set; }
        public bool ExemptEarlyEnter { get; set; }
        public bool ExemptOvertime { get; set; }
        public bool? IsCheckInAutoFilled { get; set; }
        public bool? IsCheckOutAutoFilled { get; set; }
        public string Status =>
IsHoliday ? "عطلة" :
HasLeave ? "إجازة" :
IsAbsence ? "غائب" :
IsWeeklyRest ? "راحة أسبوعية" :
CheckInTime.HasValue ? "حاضر" : "لم يسجل";
    }

    public class EmployeeMonthlyReportResult
    {
            public int UserId { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string BranchName { get; set; } = "";
        public int Month { get; set; }
        public int Year { get; set; }

        public List<MonthlyAttendanceDay> Days { get; set; } = new();

        public int TotalAbsenceDays { get; set; }
        public int TotalWeeklyRestDays { get; set; }
        public int TotalHolidayDays { get; set; }
        public TimeSpan TotalLate { get; set; }
        public TimeSpan TotalOvertime { get; set; }
        public TimeSpan TotalEarlyLeave { get; set; }
        public TimeSpan TotalEarlyEnter { get; set; }
        public TimeSpan TotalWorkHours { get; set; }
    }

    public class EmployeeMonthlySummaryItem
    {
            public int UserId { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string BranchName { get; set; } = "";
        public int TotalAbsenceDays { get; set; }
        public int TotalHolidayDays { get; set; }
        public TimeSpan TotalLate { get; set; }
        public TimeSpan TotalOvertime { get; set; }
        public TimeSpan TotalWorkHours { get; set; }
    }
}