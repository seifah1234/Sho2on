// PermissionViewModel.cs
using System; using HR_Application.Helpers;

namespace HR_Application.ViewModels
{
    public class PermissionViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? PermissionType { get; set; }
        public string? PermissionTypeName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public double Duration { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public string? StatusEn { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? EmployeeDepartment { get; set; }
        public string? EmployeeJobTitle { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectionReason { get; set; }
    }
}