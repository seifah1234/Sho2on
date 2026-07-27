namespace Sho2on.Web.Constants
{
    public static class RoleNames
    {
        public const string Admin = "ادمن";
        public const string Manager = "مدير";
        public const string HROfficer = "شئون العاملين";
        public const string HRManager = "مدير شئون العاملين";
        public const string Employee = "مستخدم";

        // كل الأدوار اللي المفروض تشوف داشبورد الإدارة (مش داشبورد الموظف العادي)
        public static readonly string[] ManagementRoles =
        {
            Admin, Manager, HROfficer, HRManager
        };
    }
}