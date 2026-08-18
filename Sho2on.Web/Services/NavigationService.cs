using Sho2on.Web.Models.Navigation;

namespace Sho2on.Web.Services
{
    public class NavigationService
    {

        public List<NavigationItem> GetMenu() => new()
        {
            new() { Title = "الرئيسية", Icon = "bi-speedometer2", Url = "/" }, // بدون صلاحية = ظاهرة للجميع

            new()
            {
                Title = "الموظفين", Icon = "bi-people",
                Children = new()
                {
                    new() { Title = "بيانات الموظفين", Url = "/employees", Icon = "bi-person-lines-fill", RequiredPermission = "بيانات الموظفين" },
                    new() { Title = "إضافة موظف", Url = "/employees/add", Icon = "bi-person-plus", RequiredPermission = "إضافة موظف" },
                    new() { Title = "مستندات الموظفين", Url = "/employees/documents", Icon = "bi-file-earmark-person", RequiredPermission = "مستندات الموظفين" },
                    new() { Title = "تقييم موظف", Url = "/employees/evaluation", Icon = "bi-clipboard-check", RequiredPermission = "تقييم موظف" },
                }
            },

            new()
            {
                Title = "الحضور و الانصراف", Icon = "bi-clock-history",
                Children = new()
                {
                    new() { Title = "سجل الحضور", Url = "/attendance", Icon = "bi-calendar3", RequiredPermission = "سجل الحضور" },
                    new() { Title = "سجل البريك", Url = "/attendance/break", Icon = "bi-cup-hot", RequiredPermission = "سجل البريك" },
                    new() { Title = "معالجة الحضور", Url = "/attendance/processing", Icon = "bi-gear", RequiredPermission = "معالجة الحضور" },
                    new() { Title = "التقرير الشهري", Url = "/attendance/monthly", Icon = "bi-file-text", RequiredPermission = "التقرير الشهري" },
                    new() { Title = "تقرير شهر الموظف", Url = "/attendance/employee-report", Icon = "bi-person-badge", RequiredPermission = "تقرير شهر الموظف" },
                    new() { Title = "تغيير الوردية", Url = "/attendance/shift-change", Icon = "bi-arrow-repeat", RequiredPermission = "تغيير الوردية" },
                }
            },

            new()
            {
                Title = "إدارة الطلبات", Icon = "bi-calendar-check",
                Children = new()
                {
                    new() { Title = "طلب إجازة", Url = "/leaves/request", Icon = "bi-send", RequiredPermission = "طلب إجازة" },
                    new() { Title = "إدارة الإجازات", Url = "/leaves", Icon = "bi-list-check", RequiredPermission = "ادارة الاجازات" },
                    new() { Title = "طلب إذن", Url = "/leaves/permission-request", Icon = "bi-clock", RequiredPermission = "طلب إذن" },
                    new() { Title = "إدارة الأذونات", Url = "/leaves/permissions", Icon = "bi-check2-square", RequiredPermission = "إدارة الأذونات" },
                    new() { Title = "طلب مأمورية", Url = "/leaves/mission-request", Icon = "bi-briefcase", RequiredPermission = "طلب مأمورية" },
                    new() { Title = "إدارة المأموريات", Url = "/leaves/missions", Icon = "bi-map", RequiredPermission = "إدارة المأموريات" },
                    new() { Title = "طلب نقل إدارة", Url = "/employee/transfer-requests", Icon = "bi-arrow-left-right", RequiredPermission = "طلب نقل إدارة" },
                    new() { Title = "إدارة نقل الإدارات", Url = "/employee/transfer-requests/management", Icon = "bi-diagram-3", RequiredPermission = "إدارة نقل الإدارات" },
                }
            },

            new()
            {
                Title = "الرواتب", Icon = "bi-cash-coin",
                Children = new()
                {
                    new() { Title = "الرواتب", Url = "/salaries", Icon = "bi-cash-stack", RequiredPermission = "الرواتب" },
                    new() { Title = "استحقاقات واستقطاعات", Url = "/salaries/benefits-deductions", Icon = "bi-calculator", RequiredPermission = "استحقاقت و استقطاعات" },
                    new() { Title = "ادارة السلف", Url = "/loans", Icon = "bi-piggy-bank", RequiredPermission = "ادارة السلف" },
                    new() { Title = "صرف فوري للموظف", Url = "/salaries/off-cycle", Icon = "bi-lightning", RequiredPermission = "صرف فوري للموظف" },
                    new() { Title = "أنواع الاستحقاقات والاستقطاعات", Url = "/settings/benefit-types", Icon = "bi-sliders", RequiredPermission = "أنواع الاستحقاقات والاستقطاعات" },
                }
            },

            new()
            {
                Title = "المهام و الشات", Icon = "bi-list-task",
                Children = new()
                {
                    new() { Title = "قائمة المهام", Url = "/tasks", Icon = "bi-person-check", RequiredPermission = "قائمة المهام" },
                    new() { Title = "قائمة الشات", Url = "/chat", Icon = "bi-chat-dots", RequiredPermission = "قائمة الشات" },
                }
            },

            new()
            {
                Title = "الإعدادات", Icon = "bi-gear",
                Children = new()
                {
                    new() { Title = "الاعدادات العامة", Url = "/settings/general", Icon = "bi-sliders", RequiredPermission = "الاعدادات العامة" },
                    new() { Title = "الفروع", Url = "/settings/branches", Icon = "bi-building", RequiredPermission = "الفروع" },
                    new() { Title = "الإدارات", Url = "/settings/departments", Icon = "bi-diagram-3", RequiredPermission = "الادارات" },
                    new() { Title = "المناطق", Url = "/settings/areas", Icon = "bi-geo-alt", RequiredPermission = "المناطق" },
                    new() { Title = "المسؤولون", Url = "/settings/officials", Icon = "bi-person", RequiredPermission = "المسؤولون" },
                    new() { Title = "المؤهلات", Url = "/settings/qualifications", Icon = "bi-mortarboard-fill", RequiredPermission = "المؤهلات" },
                    new() { Title = "الورديات", Url = "/settings/shifts", Icon = "bi-clock", RequiredPermission = "الورديات" },
                    new() { Title = "الاجازات الرسمية", Url = "/settings/official-holidays", Icon = "bi-calendar-check", RequiredPermission = "الاجازات الرسمية" },
                    new() { Title = "الراحة الأسبوعية", Url = "/settings/week-holidays", Icon = "bi-calendar-check", RequiredPermission = "الراحة الأسبوعية" },
                    new() { Title = "الوظائف", Url = "/settings/job-titles", Icon = "bi-briefcase", RequiredPermission = "الوظائف" },
                    new() { Title = "فترات الراحة", Url = "/settings/breaks", Icon = "bi-cup-hot", RequiredPermission = "فترات الراحة" },
                    new() { Title = "مستندات الشركة", Url = "/settings/company-documents", Icon = "bi-building", RequiredPermission = "مستندات الشركة" },
                    new() { Title = "أنواع الإجازات", Url = "/settings/leave-types", Icon = "bi-calendar-range", RequiredPermission = "أنواع الاجازات" },
                    new() { Title = "الأدوار والصلاحيات", Url = "/settings/roles", Icon = "bi-shield-lock", RequiredPermission = "الأدوار والصلاحيات" },
                    new() { Title = "صلاحيات المستخدم", Url = "/settings/user-permissions", Icon = "bi-person-gear", RequiredPermission = "صلاحيات المستخدم" },
                }
            },
        };

    }
}