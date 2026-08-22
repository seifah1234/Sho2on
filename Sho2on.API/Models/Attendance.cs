using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.API.Models
{
    public class Attendance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public int? ShiftId { get; set; }
        public int? LeaveId { get; set; }
        public int? PermissionId { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public TimeSpan? Late { get; set; }

        public TimeSpan? EarlyLeave { get; set; }

        public TimeSpan? EarlyEnter { get; set; }

        public TimeSpan? Overtime { get; set; }

        public TimeSpan? TotalWorkHours { get; set; }

        public int? CheckInBranchId { get; set; }
        public string? CheckInLocation { get; set; }
        public string? CheckOutLocation { get; set; }
        public int? CheckOutBranchId { get; set; }
        public double? CheckInLatitude { get; set; }
        public double? CheckInLongitude { get; set; }
        public double? CheckOutLatitude { get; set; }
        public double? CheckOutLongitude { get; set; }

        public bool ExemptLate { get; set; }
        public bool IsHoliday { get; set; }
        public bool IsAbsence { get; set; }

        public bool ExemptEarlyLeave { get; set; }

        public bool ExemptOvertime { get; set; }

        public bool ExemptEarlyEnter { get; set; }
        // في نموذج Attendance
        public int? CheckInFingerPrintId { get; set; }
        public int? CheckOutFingerPrintId { get; set; }

        [ForeignKey(nameof(CheckInFingerPrintId))]
        public FingerPrint? CheckInFingerPrint { get; set; }

        [ForeignKey(nameof(CheckOutFingerPrintId))]
        public FingerPrint? CheckOutFingerPrint { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(CheckInBranchId))]
        public Branch? CheckInBranch { get; set; }

        [ForeignKey(nameof(CheckOutBranchId))]
        public Branch? CheckOutBranch { get; set; }
        [ForeignKey(nameof(LeaveId))]
        public Leave? Leave { get; set; }
        [ForeignKey(nameof(ShiftId))]
        public Shift? Shift { get; set; }
        [ForeignKey(nameof(PermissionId))]
        public EmployeePermission? Permission { get; set; }
    }
}
