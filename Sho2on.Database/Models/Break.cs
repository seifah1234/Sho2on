using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{

    public enum BreakType
    {
        Fixed = 1,      // معاد ثابت (مثلاً 1:00 ظهرًا - 1:30 ظهرًا لكل الموظفين)
        Flexible = 2    // مدة محددة، الموظف ياخدها وقت ما يحب على مدار الشيفت
    }
    public class Break
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        public BreakType Type { get; set; } = BreakType.Fixed;

        // Start time of break
        [Required]
        public TimeSpan? StartTime { get; set; }

        // End time of break
        [Required]
        public TimeSpan? EndTime { get; set; }


        public int? DurationMinutes { get; set; }

        // Edited Date (EdDate)
        public DateTime? EditedAt { get; set; } = DateTime.Now;
    }
}
