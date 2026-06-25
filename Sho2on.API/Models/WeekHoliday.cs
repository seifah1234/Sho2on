using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.Database.Models
{
    public class WeekHoliday
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        public bool Day1 { get; set; }

        [Required]
        public bool Day2 { get; set; }

        [Required]
        public bool Day3 { get; set; }

        [Required]
        public bool Day4 { get; set; }

        [Required]
        public bool Day5 { get; set; }

        [Required]
        public bool Day6 { get; set; }

        [Required]
        public bool Day7 { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime EditedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string DaysSummary
        {
            get
            {
                var days = new List<string>();
                if (Day2) days.Add("الأحد");
                if (Day3) days.Add("الإثنين");
                if (Day4) days.Add("الثلاثاء");
                if (Day5) days.Add("الأربعاء");
                if (Day6) days.Add("الخميس");
                if (Day7) days.Add("الجمعة");
                if (Day1) days.Add("السبت");

                return days.Count > 0 ? string.Join("، ", days) : "لا توجد أيام إجازة";
            }
        }
    }
}
