using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class EmployeeAuditLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>الموظف اللي اتعدل عليه</summary>
        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public User? Employee { get; set; }

        /// <summary>نسخة من اسم الموظف وقت التعديل (لو اتحذف أو اتغير اسمه)</summary>
        [MaxLength(200)]
        public string EmployeeName { get; set; } = "";

        /// <summary>نسخة من كود الموظف</summary>
        [MaxLength(50)]
        public string? EmployeeCode { get; set; }

        /// <summary>المستخدم اللي عمل التعديل</summary>
        public int? ChangedById { get; set; }

        [ForeignKey(nameof(ChangedById))]
        public User? ChangedBy { get; set; }

        /// <summary>نسخة من اسم المستخدم</summary>
        [MaxLength(200)]
        public string ChangedByName { get; set; } = "";

        /// <summary>نوع العملية: Create / Update / Archive / Restore / Delete</summary>
        public AuditAction Action { get; set; }

        /// <summary>الحقول اللي اتغيرت بصيغة JSON: [{Field, OldValue, NewValue}]</summary>
        public string? ChangesJson { get; set; }

        /// <summary>ملاحظات إضافية</summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? IpAddress { get; set; }
    }

    public enum AuditAction
    {
        Create = 1,
        Update = 2,
        Archive = 3,
        Restore = 4,
        Delete = 5
    }

    /// <summary>تفاصيل حقل واحد اتغير</summary>
    public class FieldChange
    {
        public string Field { get; set; } = "";
        public string FieldLabel { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}