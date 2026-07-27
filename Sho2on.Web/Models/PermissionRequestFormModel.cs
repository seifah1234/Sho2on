using System.ComponentModel.DataAnnotations;

namespace Sho2on.Web.Models;

public class PermissionRequestFormModel
{
    [Range(1, int.MaxValue, ErrorMessage = "اختر الموظف")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "اختر نوع الإذن")]
    public string PermissionType { get; set; } = "";

    [Required(ErrorMessage = "حدد تاريخ الإذن")]
    public DateTime? Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "حدد وقت البداية")]
    public string StartTime { get; set; } = "";

    [Required(ErrorMessage = "حدد وقت النهاية")]
    public string EndTime { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "اختر المسؤول عن الاعتماد")]
    public int? ApproverId { get; set; }

    [Required(ErrorMessage = "سبب الإذن مطلوب")]
    [StringLength(500)]
    public string Reason { get; set; } = "";

    [StringLength(500)]
    public string? Notes { get; set; }
}