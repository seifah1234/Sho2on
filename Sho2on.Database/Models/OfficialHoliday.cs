using System.ComponentModel.DataAnnotations;

namespace Sho2on.Database.Models
{
    public class OfficialHoliday
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = "";

        [Required]
        public DateTime Date { get; set; }
        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}