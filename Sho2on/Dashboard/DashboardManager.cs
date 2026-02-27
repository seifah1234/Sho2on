using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace HR_Application.Dashboard
{
    public class DashboardManager
    {
        public System.Windows.Controls.UserControl GetDashboardWindow()
        {
            var currentUser = App.CurrentUser;
            var userPermissions = App.userPermissions ?? new List<string>();
            return new AdminDashboard();

            // تحديد نوع Dashboard حسب الصلاحيات
            if (IsAdmin(currentUser, userPermissions))
            {
                return new AdminDashboard();
            }
            else if (IsManager(userPermissions))
            {
                return new ManagerDashboard();
            }
            else if (IsHROfficer(userPermissions))
            {
                return new HRDashboard();
            }
            else if (IsRegularEmployee(userPermissions))
            {
                return new EmployeeDashboard();
            }
            else
            {
                // Default fallback
                return new EmployeeDashboard();
            }
        }

        private static bool IsAdmin(User user, List<string> permissions)
        {
            if (user == null) return false;

            // رئيس الإدارة أو مسؤول النظام
            bool isSuperAdmin = user.FullName == "OR" ||
                              user.Code == "0" ||
                              user.Code == "1";

            bool hasAdminPermissions = permissions.Contains("مدير_النظام") ||
                                     permissions.Contains("الصلاحيات") ||
                                     permissions.Contains("الاعدادات العامة");

            return isSuperAdmin || hasAdminPermissions || permissions.Count >= 15;
        }

        private static bool IsManager(List<string> permissions)
        {
            var managerPermissions = new[]
            {
                "التقارير",
                "الحضور و الانصراف",
                "ادارة ماليات",
                "بيانات العاملين",
                "شئون العاملين",
                "المرتبات"
            };

            return managerPermissions.Count(p => permissions.Contains(p)) >= 3;
        }

        private static bool IsHROfficer(List<string> permissions)
        {
            var hrPermissions = new[]
            {
                "شئون العاملين",
                "بيانات العاملين",
                "الاجراءات",
                "ادارة الاجازات",
                "تقرير المرتبات",
                "اضافة موظف"
            };

            return hrPermissions.Count(p => permissions.Contains(p)) >= 3;
        }

        private static bool IsRegularEmployee(List<string> permissions)
        {
            // إذا لم يكن لديه أي من الصلاحيات الإدارية
            var adminPermissions = new[]
            {
                "التقارير", "الحضور و الانصراف", "ادارة ماليات",
                "بيانات العاملين", "شئون العاملين", "الصلاحيات",
                "الاعدادات العامة", "FingerPrints", "Settings"
            };

            return !adminPermissions.Any(p => permissions.Contains(p));
        }

        public static async Task<DashboardStats> GetDashboardStatsAsync(int userId)
        {
            using var context = new AppDbContext(App.ConnectionString);
            var stats = new DashboardStats();

            try
            {
                // إحصائيات عامة
                stats.TotalEmployees = await context.Users
                    .Where(u => !u.IsArchived)
                    .CountAsync();

                var today = DateTime.Today;
                stats.TodayAttendance = await context.Attendances
                    .Where(a => a.AttendanceDate.Date == today &&
                               a.CheckInTime.HasValue)
                    .CountAsync();

                stats.TodayAbsence = await context.Attendances
                    .Where(a => a.AttendanceDate.Date == today &&
                               a.IsAbsence)
                    .CountAsync();

                stats.PendingLeaves = await context.Leaves
                    .Where(l => l.Status == 0)
                    .CountAsync();

                // إحصائيات حسب المستخدم
                var user = await context.Users
                    .Include(u => u.JobTitle)
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null)
                {
                    stats.UserName = user.FullName;
                    stats.UserJob = user.JobTitle?.Name;
                    stats.UserDepartment = user.Department?.Name;

                    // رصيد الإجازات
                    var leaveBalance = await context.LeaveBalances
                        .FirstOrDefaultAsync(lb => lb.UserId == userId);
                    stats.LeaveBalance = (leaveBalance?.TotalBalance ?? 0) - (leaveBalance?.UsedBalance ?? 0);
                }

                return stats;
            }
            catch (Exception)
            {
                return stats;
            }
        }
    }

    public class DashboardStats
    {
        public int TotalEmployees { get; set; }
        public int TodayAttendance { get; set; }
        public int TodayAbsence { get; set; }
        public int PendingLeaves { get; set; }
        public string UserName { get; set; }
        public string UserJob { get; set; }
        public string UserDepartment { get; set; }
        public int LeaveBalance { get; set; }
    }
}