using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.API.Models
{
    public class Break
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        // Start time of break
        [Required]
        public TimeSpan StartTime { get; set; }

        // End time of break
        [Required]
        public TimeSpan EndTime { get; set; }

        // Edited Date (EdDate)
        public DateTime? EditedAt { get; set; } = DateTime.Now;
    }
}
