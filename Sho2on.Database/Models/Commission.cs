using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.Database.Models
{
    public class Commission
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; } = "Target"; // Target, External
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
