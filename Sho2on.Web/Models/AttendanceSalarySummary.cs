namespace Sho2on.Web.Models
{
    public class AttendanceSalaryDay
    {
        public DateOnly Date { get; set; }
        public bool IsAbsence { get; set; }
        public bool IsHoliday { get; set; }
        public TimeSpan? Late { get; set; }
        public TimeSpan? Overtime { get; set; }
        public TimeSpan? EarlyLeave { get; set; }
        public decimal LateDeduction { get; set; }
        public decimal OvertimeBonus { get; set; }
        public string? Description { get; set; }
    }

    public class AttendanceSalarySummary
    {
        public int UserId { get; set; }
        public string EmployeeName { get; set; } = "";
        public int AbsenceDays { get; set; }
        public int WeeklyRestDays { get; set; }
        public decimal TotalLateValue { get; set; }
        public decimal MainSalary { get; set; }
        public decimal MinSalary { get; set; }
        public decimal TotalOvertimeValue { get; set; }
        public decimal AbsenceDeductionValue { get; set; }
        public decimal NetAttendanceAdjustment => TotalOvertimeValue - TotalLateValue - AbsenceDeductionValue;
        public List<AttendanceSalaryDay> Days { get; set; } = new();
    }
}