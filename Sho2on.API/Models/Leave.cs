using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.API.Models
{
    public class Leave
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int LeaveType { get; set; }

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
        public int Status { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        public DateTime? ApprovalDate { get; set; }

        public int? ApprovedBy { get; set; }

        public string? RejectionReason { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        [ForeignKey(nameof(ApprovedBy))]
        public virtual User Approver { get; set; }
    }
}