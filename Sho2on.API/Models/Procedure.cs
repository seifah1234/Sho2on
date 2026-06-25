using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.Database.Models
{
    public class Procedure
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string? Notes { get; set; }
        public string? Status { get; set; } = "Pending";

        [Required]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public DateTime? StartDate { get; set; }

        [Required]
        public DateTime? EndDate { get; set; }

        [Required]
        public int? Type { get; set; }

        [Required]
        public int? UserId { get; set; }

        public int? ApprovedByUserId { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public int? BranchId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }

        [ForeignKey("ApprovedByUserId")]
        public virtual User? ApprovedBy { get; set; }

    }
    public static class ProcedureStatus
    {
        public const string UnderReview = "Under Review";
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }
}
