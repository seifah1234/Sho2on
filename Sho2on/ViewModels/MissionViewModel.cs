using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Application.ViewModels
{
    public class MissionViewModel
    {
        public int? Id { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public double? Duration { get; set; }
        public string? Status { get; set; }
        public string? StatusEn { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? EmployeeDepartment { get; set; }
        public string? EmployeeJobTitle { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectionReason { get; set; }
    }
}
