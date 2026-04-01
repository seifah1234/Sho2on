using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class Leave
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [ForeignKey(nameof(LeaveType))]
        public int LeaveTypeId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int Duration { get; set; }

        [Required]
        public string Reason { get; set; }

        public string? Notes { get; set; }

        [Required]
        public int Status { get; set; } // 0: Draft, 1: Pending, 2: Approved, 3: Rejected, 4: Cancelled

        [Required]
        public DateTime RequestDate { get; set; }

        public DateTime? ApprovalDate { get; set; }

        public int? ApprovedBy { get; set; }
        public int? ReplacementUserId { get; set; }

        public string? RejectionReason { get; set; }

        public bool IsCancelled { get; set; } = false;
        public DateTime? CancelledDate { get; set; }
        public int? CancelledBy { get; set; }
        public string? CancellationReason { get; set; }

        // Navigation
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        [ForeignKey(nameof(LeaveTypeId))]
        public virtual LeaveType LeaveType { get; set; }

        [ForeignKey(nameof(ApprovedBy))]
        public virtual User Approver { get; set; }

        [ForeignKey(nameof(ReplacementUserId))]
        public virtual User ReplacementUser { get; set; }

        [ForeignKey(nameof(CancelledBy))]
        public virtual User Canceller { get; set; }
    }
}