using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public partial class DashboardService
    {
        private readonly AppDbContext _db;
        public DashboardService(AppDbContext db) => _db = db;

        // ══ الشجرة الهرمية: الشركة → القطاعات (Degrees) → الفروع → الإدارات ══

        public async Task<DashboardStats> GetPersonalStatsAsync(int userId)
        {
            var stats = new DashboardStats();

            var user = await _db.Users
                .Include(u => u.JobTitle)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
            {
                stats.UserName = user.FullName;
                stats.UserJob = user.JobTitle?.Name;
                stats.UserDepartment = user.Department?.Name;

                var leaveBalance = await _db.LeaveBalances.FirstOrDefaultAsync(lb => lb.UserId == userId);
                stats.LeaveBalance = (leaveBalance?.TotalBalance ?? 0) - (leaveBalance?.UsedBalance ?? 0);
            }

            return stats;
        }

        public async Task<DashboardTreeNode> GetCompanyTreeAsync()
        {
            var companyName = await _db.Settings.Select(s => s.CompanyName).FirstOrDefaultAsync() ?? "الشركة";
            var sectors = await _db.Degrees.Include(d => d.Users).OrderBy(d => d.Name).ToListAsync();
            var branches = await _db.Branches.Include(b => b.Users).OrderBy(b => b.Name).ToListAsync();
            var depts = await _db.Departments.Include(d => d.Users).OrderBy(d => d.Name).ToListAsync();

            var root = new DashboardTreeNode
            {
                Name = companyName,
                Type = "الشركة",
                ChildrenType = "قطاعات",
                TotalEmployees = sectors.Sum(s => s.Users.Count(u => !u.IsArchived)),
                TotalChildren = sectors.Count,
                Children = sectors.Select(s => new DashboardTreeNode
                {
                    Id = s.Id,
                    Name = s.Name,
                    Type = "قطاع",
                    ChildrenType = "فروع",
                    TotalEmployees = s.Users.Count(u => !u.IsArchived),
                    TotalChildren = branches.Count(b => b.Users.Any(u => u.DegreeId == s.Id)),
                    Children = branches.Where(b => b.Users.Any(u => u.DegreeId == s.Id)).Select(b => new DashboardTreeNode
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Type = "فرع",
                        ChildrenType = "إدارات",
                        TotalEmployees = b.Users.Count(u => !u.IsArchived && u.DegreeId == s.Id),
                        TotalChildren = depts.Count(d => d.Users.Any(u => u.DegreeId == s.Id && u.BranchId == b.Id)),
                        Children = depts.Where(d => d.Users.Any(u => u.DegreeId == s.Id && u.BranchId == b.Id)).Select(d => new DashboardTreeNode
                        {
                            Id = d.Id,
                            Name = d.Name,
                            Type = "إدارة",
                            TotalEmployees = d.Users.Count(u => !u.IsArchived && u.DegreeId == s.Id && u.BranchId == b.Id && u.DepartmentId == d.Id),
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            return root;
        }

        // ══ بيانات الرسوم البيانية الأربعة ══
        public async Task<DashboardChartsData> GetChartsDataAsync()
        {
            var data = new DashboardChartsData();

            data.MaleCount = await _db.Users.Where(u => !u.IsArchived && u.Gender == 'M').CountAsync();
            data.FemaleCount = await _db.Users.Where(u => !u.IsArchived && u.Gender == 'F').CountAsync();

            var depts = await _db.Departments.Include(d => d.Users)
                .OrderByDescending(d => d.Users.Count(u => !u.IsArchived))
                .Take(8).ToListAsync();
            data.DepartmentLabels = depts.Select(d => d.Name).ToList();
            data.DepartmentCounts = depts.Select(d => d.Users.Count(u => !u.IsArchived)).ToList();

            var branches = await _db.Branches.Include(b => b.Users)
                .OrderByDescending(b => b.Users.Count(u => !u.IsArchived))
                .Take(10).ToListAsync();
            data.BranchLabels = branches.Select(b => b.Name).ToList();
            data.BranchCounts = branches.Select(b => b.Users.Count(u => !u.IsArchived)).ToList();

            var sectors = await _db.Degrees.Include(d => d.Users)
                .OrderByDescending(d => d.Users.Count(u => !u.IsArchived))
                .ToListAsync();
            data.SectorLabels = sectors.Select(s => s.Name).ToList();
            data.SectorCounts = sectors.Select(s => s.Users.Count(u => !u.IsArchived)).ToList();

            return data;
        }

        // ══ التنبيهات ══
        public async Task<List<DashboardAlert>> GetAlertsAsync()
        {
            var alerts = new List<DashboardAlert>();

            var expiringDocs = await _db.Users
                .Where(u => u.NationalIDExpiration.HasValue &&
                            u.NationalIDExpiration.Value < DateOnly.FromDateTime(DateTime.Now.AddMonths(1)))
                .Take(5).ToListAsync();
            foreach (var u in expiringDocs)
                alerts.Add(new DashboardAlert { Icon = "bi-person-vcard", Message = $"رقم قومي منتهي للموظف {u.FullName}" });

            var pendingLoans = await _db.Loans.Where(l => l.Status == "SentToManager").CountAsync();
            if (pendingLoans > 0)
                alerts.Add(new DashboardAlert { Icon = "bi-cash-stack", Message = $"{pendingLoans} طلب سلفة بانتظار الموافقة" });

            var totalUsers = await _db.Users.CountAsync();
            var todayAttendance = await _db.Attendances
                .Where(a => a.AttendanceDate.Date == DateTime.Today && a.CheckInTime.HasValue)
                .CountAsync();
            if (totalUsers > 0 && todayAttendance < totalUsers * 0.8)
                alerts.Add(new DashboardAlert { Icon = "bi-graph-down", Message = "معدل الحضور اليومي منخفض" });

            return alerts;
        }
    }
}