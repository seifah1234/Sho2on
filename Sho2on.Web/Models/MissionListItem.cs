namespace Sho2on.Web.Models;

public class MissionListItem
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string EmployeeCode { get; set; } = "";
    public string EmployeeDepartment { get; set; } = "";
    public string EmployeeJobTitle { get; set; } = "";
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public double Duration { get; set; }
    public string Status { get; set; } = "";
    public string StatusText { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string ApprovedByName { get; set; } = "";
    public DateTime? ApprovedDate { get; set; }
    public string? Notes { get; set; }
    public string? BranchName { get; set; }
}