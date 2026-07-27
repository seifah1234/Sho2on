using System.ComponentModel.DataAnnotations;

namespace Sho2on.Web.Models;

public class LeaveRequestFormModel
{
    [Range(1, int.MaxValue, ErrorMessage = "اختر الموظف")]
    public int UserId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "اختر نوع الإجازة")]
    public int LeaveTypeId { get; set; }

    [Required(ErrorMessage = "حدد تاريخ البداية")]
    public DateTime? StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "حدد تاريخ النهاية")]
    public DateTime? EndDate { get; set; } = DateTime.Today;

    public int? ReplacementUserId { get; set; }

    // الموظف المسؤول عن اعتماد الطلب
    public int? ApproverId { get; set; }

    [Required(ErrorMessage = "سبب الإجازة مطلوب")]
    [StringLength(500, ErrorMessage = "السبب لا يزيد عن 500 حرف")]
    public string Reason { get; set; } = "";

    [StringLength(500)]
    public string? Notes { get; set; }
}