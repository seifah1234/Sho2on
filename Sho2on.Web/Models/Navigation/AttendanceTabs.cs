namespace Sho2on.Web.Models.Navigation
{
    public static class AttendanceTabs
    {
        public static readonly IReadOnlyList<ModuleTab> Items =
        [
            new()
            {
                Title = "سجل الحضور",
                Url = "/attendance",
                Icon = "bi bi-calendar-check"
            },
            new()
            {
                Title = "معالجة الحضور",
                Url = "/attendance/processing",
                Icon = "bi bi-magic"
            },
            new()
            {
                Title = "التقرير الشهري",
                Url = "/attendance/monthly",
                Icon = "bi bi-calendar3"
            },
            new()
            {
                Title = "تقرير شهر الموظف",
                Url = "/attendance/employee-report",
                Icon = "bi bi-person-lines-fill"
            },
            new()
            {
                Title = "تغيير الوردية",
                Url = "/attendance/shift-change",
                Icon = "bi bi-arrow-repeat"
            }
        ];
    }
}