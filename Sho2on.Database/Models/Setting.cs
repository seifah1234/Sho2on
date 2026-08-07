using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.Database.Models
{
    public class Setting
    {
        [Key]
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int MaxMobileUsers { get; set; } = 0;

        public string? CentralDocumentStoragePath { get; set; } = string.Empty;

        // ══ إعدادات التأخير والإضافي ══
        public int LateOvertimeCalculationMode { get; set; } = 0; // 0 = نسبة من الحد الأدنى (دقائق)، 1 = مبلغ ثابت (مالية)

        // قيمة التأخير المتكرر
        public decimal LateValue { get; set; } = 0;

        // عدد مرات التأخير قبل تطبيق العقوبة
        public int LateRepeat { get; set; } = 0;

        // ══ إعدادات الشهر ══
        public int StartOfMonth { get; set; } = 26;
        public int EndOfMonth { get; set; } = 25;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}