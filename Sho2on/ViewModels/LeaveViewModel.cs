using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.ComponentModel;

namespace HR_Application.ViewModels
{
    public class LeaveViewModel
    {
        public int Id { get; set; }

        [DisplayName("كود الموظف")]
        public int EmployeeId { get; set; }

        [DisplayName("اسم الموظف")]
        public string EmployeeName { get; set; }

        [DisplayName("القائم عن العمل")]
        public string? ReplacementUserName { get; set; }

        [DisplayName("نوع الإجازة")]
        public string LeaveTypeName { get; set; }

        [DisplayName("من تاريخ")]
        public DateTime StartDate { get; set; }

        [DisplayName("إلى تاريخ")]
        public DateTime EndDate { get; set; }

        [DisplayName("المدة")]
        public int Duration { get; set; }

        [DisplayName("السبب")]
        public string Reason { get; set; }

        [DisplayName("الحالة")]
        public string Status { get; set; }

        [DisplayName("تاريخ الطلب")]
        public DateTime RequestDate { get; set; }

        public int StatusId { get; set; }
        public int LeaveTypeId { get; set; }
    }

    public class LeaveFilterViewModel
    {
        public int? EmployeeId { get; set; }
        public int? LeaveTypeId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Status { get; set; }
    }
}