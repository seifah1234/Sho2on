using Sho2on.Web.Models.Navigation;

namespace Sho2on.Web.Services
{
    public class NavigationService
    {
        public List<NavigationItem> GetMenu() => new()
        {
            new() { Title = "الرئيسية", Icon = "bi-speedometer2", Url = "/" },

            new()
            {
                Title = "الموظفين", Icon = "bi-people",
                Children = new()
                {
                    new() { Title = "بيانات الموظفين", Url = "/employees", Icon = "bi-person-lines-fill" },
                    new() { Title = "إضافة موظف", Url = "/employees/add", Icon = "bi-person-plus" },
                    new() { Title = "مستندات الموظفين", Url = "/employees/documents", Icon = "bi-file-earmark-person" },
                    new() { Title = "تقييم موظف", Url = "/employees/evaluation", Icon = "bi-clipboard-check" },
                }
            },

            new()
            {
                Title = "الحضور و الانصراف", Icon = "bi-clock-history",
                Children = new()
                {
                    new() { Title = "سجل الحضور", Url = "/attendance", Icon = "bi-calendar3" },
                    new() { Title = "سجل البريك", Url = "/attendance/break", Icon = "bi-cup-hot" },
                    new() { Title = "معالجة الحضور", Url = "/attendance/processing", Icon = "bi-gear" },
                    new() { Title = "التقرير الشهري", Url = "/attendance/monthly", Icon = "bi-file-text" },
                    new() { Title = "تقرير شهر الموظف", Url = "/attendance/employee-report", Icon = "bi-person-badge" },
                    new() { Title = "تغيير الوردية", Url = "/attendance/shift-change", Icon = "bi-arrow-repeat" },
                }
            },

            new()
            {
                Title = "إدارة الطلبات", Icon = "bi-calendar-check",
                Children = new()
                {
                    new() { Title = "طلب إجازة", Url = "/leaves/request", Icon = "bi-send" },
                    new() { Title = "إدارة الإجازات", Url = "/leaves", Icon = "bi-list-check" },
                    new() { Title = "طلب إذن", Url = "/leaves/permission-request", Icon = "bi-clock" },
                    new() { Title = "إدارة الأذونات", Url = "/leaves/permissions", Icon = "bi-check2-square" },
                    new() { Title = "طلب مأمورية", Url = "/leaves/mission-request", Icon = "bi-briefcase" },
                    new() { Title = "إدارة المأموريات", Url = "/leaves/missions", Icon = "bi-map" },
                    new() { Title = "طلب نقل إدارة", Url = "/employee/transfer-requests", Icon = "bi-arrow-left-right" },
                    new() { Title = "إدارة نقل الإدارات", Url = "/emplyee/transfer-requests/management", Icon = "bi-diagram-3" },
                }
            },

            new()
            {
                Title = "الرواتب", Icon = "bi-cash-coin",
                Children = new()
                {
                    new() { Title = "الرواتب", Url = "/salaries", Icon = "bi-cash-stack" },
                    new() { Title = "تفاصيل مالية الموظف", Url = "/salaries/details", Icon = "bi-wallet2" },
                    new() { Title = "استحقاقات واستقطاعات", Url = "/salaries/benefits-deductions", Icon = "bi-calculator" },
                    new() { Title = "السلف", Url = "/loans", Icon = "bi-piggy-bank" },
                    new() { Title = "صرف فوري للموظف", Url = "/salaries/off-cycle", Icon = "bi-lightning" },
                    new() { Title = "أنواع الاستحقاقات والاستقطاعات", Url = "/settings/benefit-types", Icon = "bi-sliders" },
                }
            },

            new()
            {
                Title = "المهام و الشات", Icon = "bi-list-task",
                Children = new()
                {
                    new() { Title = "قائمة المهام", Url = "/tasks", Icon = "bi-person-check" },
                    new() { Title = "قائمة الشات", Url = "/chat", Icon = "bi-chat-dots" },
                }
            },

            new()
            {
                Title = "الإعدادات", Icon = "bi-gear",
                Children = new()
                {
                    new() { Title = "إعدادات عامة", Url = "/settings/general", Icon = "bi-sliders" },
                    new() { Title = "الفروع", Url = "/settings/branches", Icon = "bi-building" },
                    new() { Title = "الإدارات", Url = "/settings/departments", Icon = "bi-diagram-3" },
                    new() { Title = "المناطق", Url = "/settings/areas", Icon = "bi-geo-alt" },
                    new() { Title = "المسؤولون", Url = "/settings/officials", Icon = "bi-person" },
                    new() { Title = "المؤهلات", Url = "/settings/qualifications", Icon = "bi-graduation-cap" },
                    new() { Title = "الورديات", Url = "/settings/shifts", Icon = "bi-clock" },
                    new() { Title = "الاجازات الرسمية", Url = "/settings/official-holidays", Icon = "bi-calendar-check" },
                    new() { Title = "الراحة الأسبوعية", Url = "/settings/week-holidays", Icon = "bi-calendar-check" },
                    new() { Title = "الوظائف", Url = "/settings/job-titles", Icon = "bi-briefcase" },
                    new() { Title = "فترات الراحة", Url = "/settings/breaks", Icon = "bi-cup-hot" },
                    new() { Title = "مستندات الشركة", Url = "/settings/company-documents", Icon = "bi-building" },
                    new() { Title = "أنواع الإجازات", Url = "/settings/leave-types", Icon = "bi-calendar-range" },
                }
            },
        };
    }
}