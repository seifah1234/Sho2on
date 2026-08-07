namespace Sho2on.Web.Models
{
    public class AttendanceProcessingResult
    {
        public int EmployeesProcessed { get; set; }
        public int DaysAutoResolved { get; set; }
        public int DuplicateScansRemoved { get; set; }
        public int MissingPunchesAutoFilled { get; set; }
        public int StatusesAutoCorrected { get; set; }      // جديد
        public int SingleScanDaysAutoResolved { get; set; }  // جديد
        public List<AttendanceReviewItem> NeedsReview { get; set; } = new();
    }

    public class RawScanItem
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public DateTime ScanTime { get; set; }
        public bool IsCheckIn { get; set; }   // Status == 1
        public bool IsManualEntry { get; set; }   // Status == 1
    }

    public class AttendanceReviewItem
    {
        public int AttendanceId { get; set; }
        public int UserId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public DateOnly Date { get; set; }
        public DateTime ScanTime { get; set; }
        public string GuessedAs { get; set; } = "";   // "حضور" أو "انصراف"
        public string Reason { get; set; } = "";
    }
}