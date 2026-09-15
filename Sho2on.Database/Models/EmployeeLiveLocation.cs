using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    // آخر موقع معروف لكل موظف (صف واحد لكل موظف، بيتحدّث باستمرار أثناء وردية العمل)
    public class EmployeeLiveLocation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        // دقة القراءة بالمتر (من الـ GPS)، اختياري
        public double? Accuracy { get; set; }

        // آخر وقت وصل فيه تحديث موقع من موبايل الموظف
        [Required]
        public DateTime UpdatedAtUtc { get; set; }

        // هل الموظف حاضر (Checked-in) وقت آخر تحديث
        public bool IsOnShift { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
