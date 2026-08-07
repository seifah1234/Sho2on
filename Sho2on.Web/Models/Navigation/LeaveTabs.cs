namespace Sho2on.Web.Models.Navigation;

public static class LeaveTabs
{
    public static readonly IReadOnlyList<ModuleTab> Items =
    [
        new()
        {
            Title = "إدارة الإجازات",
            Url = "/leaves",
            Icon = "bi bi-calendar-check"
        },
        new()
        {
            Title = "طلب إجازة جديد",
            Url = "/leaves/request",
            Icon = "bi bi-plus-circle"
        },
        new()
        {
            Title = "أرصدة الإجازات",
            Url = "/leaves/balances",
            Icon = "bi bi-wallet2"
        },
        new()
        {
            Title = "أنواع الإجازات",
            Url = "/settings/leave-types",
            Icon = "bi bi-tags"
        },
        new()
        {
            Title = "إدارة الأذونات",
            Url = "/leaves/permissions",
            Icon = "bi bi-clock-history"
        },
        new()
        {
            Title = "طلب إذن جديد",
            Url = "/leaves/permission-request",
            Icon = "bi bi-clock-plus"
        },
        new()
        {
            Title = "إدارة المأموريات",
            Url = "/leaves/missions",
            Icon = "bi bi-briefcase"
        },
        new()
        {
            Title = "طلب مأمورية جديدة",
            Url = "/leaves/mission-request",
            Icon = "bi bi-briefcase-plus"
        }
    ];
}