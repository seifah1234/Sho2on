using System.ComponentModel.DataAnnotations;

namespace Sho2on.Web.Models;

public class LeaveTypeFormModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "اسم نوع الإجازة مطلوب")]
    [StringLength(100, ErrorMessage = "اسم نوع الإجازة لا يزيد عن 100 حرف")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "كود نوع الإجازة مطلوب")]
    [StringLength(10, ErrorMessage = "الكود لا يزيد عن 10 حروف")]
    public string Code { get; set; } = "";

    [Range(0, 365, ErrorMessage = "الرصيد يجب أن يكون بين 0 و365")]
    public int DefaultBalance { get; set; }

    public bool IsActive { get; set; } = true;
    public bool DeductFromBalance { get; set; } = true;
    public bool RequiresApproval { get; set; } = true;

    [Range(1, 365, ErrorMessage = "الحد الأقصى يجب أن يكون بين 1 و365")]
    public int? MaxConsecutiveDays { get; set; }

    [StringLength(500, ErrorMessage = "الملاحظات لا تزيد عن 500 حرف")]
    public string? Notes { get; set; }
}