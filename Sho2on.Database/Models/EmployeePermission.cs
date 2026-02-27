// EmployeePermission.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    [Table("EmployeePermissions")]
    public class EmployeePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PermissionType { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public DateTime EndDateTime { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public double Duration { get; set; } // بالساعات

        [MaxLength(500)]
        public string Reason { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        public decimal? DeductedAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        public int? ApprovedByUserId { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        [Required]
        public int BranchId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("ApprovedByUserId")]
        public virtual User? ApprovedBy { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }
    }

    public static class PermissionTypes
    {
        public const string EarlyLeave = "EarlyLeave"; // خروج مبكر
        public const string LateEntry = "LateEntry"; // دخول متأخر
        public const string PersonalLeave = "PersonalLeave"; // إذن شخصي
        public const string Emergency = "Emergency"; // طارئ
        public const string Official = "Official"; // رسمي
        public const string Other = "Other"; // أخرى
    }

    public static class PermissionStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }
}