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
                    new() { Title = "بيانات الموظفين", Url = "/employees" },
                    new() { Title = "إضافة موظف", Url = "/employees/add" },
                    new() { Title = "أرشيف الموظفين", Url = "/employees/archive" },
                    new() { Title = "تقييم موظف", Url = "/employees/evaluation" },
                }
            },

            new()
            {
                Title = "الحضور و الانصراف", Icon = "bi-clock-history",
                Children = new()
                {
                    new() { Title = "سجل الحضور", Url = "/attendance" },
                    new() { Title = "التقرير الشهري", Url = "/attendance/monthly" },
                    new() { Title = "تقرير شهر الموظف", Url = "/attendance/employee-report" },
                    new() { Title = "تغيير الوردية", Url = "/attendance/shift-change" },
                }
            },

            new()
            {
                Title = "الإجازات والأذونات", Icon = "bi-calendar-check",
                Children = new()
                {
                    new() { Title = "طلب إجازة", Url = "/leaves/request" },
                    new() { Title = "إدارة الإجازات", Url = "/leaves" },
                    new() { Title = "أرصدة الإجازات", Url = "/leaves/balances" },
                    new() { Title = "طلب إذن", Url = "/leaves/permission-request" },
                    new() { Title = "إدارة الأذونات", Url = "/leaves/permissions" },
                }
            },

            new()
            {
                Title = "الرواتب", Icon = "bi-cash-coin",
                Children = new()
                {
                    new() { Title = "كشف راتب موظف", Url = "/salaries/employee" },
                    new() { Title = "كشوف الرواتب الشهرية", Url = "/salaries/monthly" },
                    new() { Title = "استحقاقات واستقطاعات", Url = "/salaries/benefits-deductions" },
                    new() { Title = "استقطاعات", Url = "/salaries/deductions" },
                    new() { Title = "المأموريات", Url = "/salaries/missions" },
                    new() { Title = "السلف", Url = "/salaries/loans" },
                    new() { Title = "صرف المرتبات الجماعي", Url = "/salaries/bulk-payment" },
                    new() { Title = "تقرير المرتبات", Url = "/salaries/report" },
                    new() { Title = "تصدير الرواتب", Url = "/salaries/export" },
                }
            },

            new()
            {
                Title = "المحادثات والمهام", Icon = "bi-chat-dots",
                Children = new()
                {
                    new() { Title = "المحادثات", Url = "/conversations" },
                    new() { Title = "المهام", Url = "/tasks" },
                }
            },

            new()
            {
                Title = "الإعدادات", Icon = "bi-gear",
                Children = new()
                {
                    new() { Title = "الفروع", Url = "/settings/branches" },
                    new() { Title = "المناطق", Url = "/settings/areas" },
                    new() { Title = "الإدارات", Url = "/settings/departments" },
                    new() { Title = "الوظائف", Url = "/settings/jobs" },
                    new() { Title = "القطاعات", Url = "/settings/degrees" },
                    new() { Title = "المؤهلات", Url = "/settings/qualifications" },
                    new() { Title = "الورديات", Url = "/settings/shifts" },
                    new() { Title = "فترات الراحة", Url = "/settings/breaks" },
                    new() { Title = "العطلات الأسبوعية", Url = "/settings/week-holidays" },
                    new() { Title = "العطلات الرسمية", Url = "/settings/official-holidays" },
                    new() { Title = "أنواع الإجازات", Url = "/settings/leave-types" },
                    new() { Title = "الأدوار", Url = "/settings/roles" },
                    new() { Title = "الصلاحيات", Url = "/settings/permissions" },
                    new() { Title = "صلاحيات المستخدم", Url = "/settings/user-permissions" },
                    new() { Title = "فروع المستخدم", Url = "/settings/user-branches" },
                    new() { Title = "مستندات الشركة", Url = "/settings/company-documents" },
                    new() { Title = "الإعدادات العامة", Url = "/settings/general" },
                }
            },
        };
    }
}