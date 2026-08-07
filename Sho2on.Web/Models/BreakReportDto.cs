using Sho2on.Database.Models;

namespace Sho2on.Web.Models
{
    public class BreakReportDto
    {
        public int UserId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalBreaks { get; set; }
        public int OnTime { get; set; } // ملتزم بالمدة
        public int Exceeded { get; set; } // تجاوز المدة
        public int TotalExtraMinutes { get; set; } // إجمالي الدقائق الإضافية
        public List<BreakLog> Logs { get; set; } = new();
    }
}