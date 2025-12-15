using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.API.Models
{
    public class LeaveBalance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public int LeaveType { get; set; }

        [Required]
        public int TotalBalance { get; set; }

        [Required]
        public int UsedBalance { get; set; }

        [Required]
        public int RemainingBalance => TotalBalance - UsedBalance;

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
    }
}
