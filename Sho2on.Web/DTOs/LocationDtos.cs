using System;

namespace Sho2on.API.Dtos
{
    // بيبعته الموبايل كل ما يحدّث موقعه
    public class UpdateLocationDto
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public bool IsOnShift { get; set; } = true;
    }

    // بيترجع للمدير: موقع كل موظف في فريقه
    public class EmployeeLocationDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? JobTitleName { get; set; }
        public string? DepartmentName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public bool IsOnShift { get; set; }

        // هل آخر تحديث حديث (خلال آخر دقيقتين مثلاً) ولا الموظف "أوفلاين"
        public bool IsRecentlyActive { get; set; }
    }
}
