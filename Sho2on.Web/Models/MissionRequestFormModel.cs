namespace Sho2on.Web.Models;

public class MissionRequestFormModel
{
    public int UserId { get; set; }
    public string? UserCode { get; set; }
    public string? UserName { get; set; }
    public DateTime? StartDate { get; set; } = DateTime.Now;
    public DateTime? EndDate { get; set; } = DateTime.Now.AddHours(1);
    public int? ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public string? Notes { get; set; }
    public bool HasPermission { get; set; } // for salary operation (type 2)
    public decimal? PermissionValue { get; set; }
}