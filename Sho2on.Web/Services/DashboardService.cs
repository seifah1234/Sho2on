using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Web.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sho2on.Web.Services
{
    public class HiredEmployeeItem
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string? Code { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? Branch { get; set; }
    public DateOnly HireDate { get; set; }
}

    public partial class DashboardService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        public DashboardService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;


        public async Task<DashboardStats> GetPersonalStatsAsync(int userId)
        {
            var stats = new DashboardStats();
            using var _db = await _dbFactory.CreateDbContextAsync();

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

        // ══════════════════════════════════════════════════════════════
//  إحصائيات التعيين والمغادرة
// ══════════════════════════════════════════════════════════════

/// <summary>
/// إحصائيات التعيين والمغادرة في فترة محددة
/// </summary>
public async Task<HiringStats> GetHiringStatsAsync(DateOnly from, DateOnly to, string selectedReason)
{
    using var _db = await _dbFactory.CreateDbContextAsync();

    // ⚠️ عدّل أسماء الحقول حسب الـ User model عندك
    var hiredInPeriod = await _db.Users
        .Where(u => u.InDuty && u.HireDate >= from && u.HireDate <= to)
        .CountAsync();

            var leftInPeriod = await _db.Users
                .Where(u => !u.InDuty || (u.FinishJob != null
                         && u.FinishJob >= from
                         && u.FinishJob <= to)).ToListAsync();
        
        if (selectedReason != "0")
        {
                leftInPeriod = leftInPeriod.Where(u => u.EndDutyTypeId.ToString() == selectedReason).ToList();
        }

    var activeEmployees = await _db.Users.CountAsync(u => !u.IsArchived && u.InDuty);
    var archivedEmployees = await _db.Users.CountAsync(u => u.IsArchived);
            var leftInPeriodCount = leftInPeriod.Count;
    // حساب المعدل الشهري
    var months = Math.Max(1, (to.ToDateTime(TimeOnly.MinValue) - from.ToDateTime(TimeOnly.MinValue)).Days / 30.0);
    var monthlyHiring = hiredInPeriod / months;
    var monthlyTurnover = activeEmployees > 0
        ? (leftInPeriodCount / months) / activeEmployees * 100
        : 0;

    return new HiringStats
    {
        HiredInPeriod = hiredInPeriod,
        LeftInPeriod = leftInPeriodCount,
        ActiveEmployees = activeEmployees,
        ArchivedEmployees = archivedEmployees,
        TotalEverEmployed = activeEmployees + archivedEmployees,
        MonthlyHiringRate = Math.Round(monthlyHiring, 1),
        MonthlyTurnoverRate = Math.Round(monthlyTurnover, 2),
    };
}

/// <summary>
/// اتجاه التعيين/المغادرة آخر 12 شهر
/// </summary>
public async Task<List<HiringTrendPoint>> GetHiringTrendAsync(int months = 12)
{
    using var _db = await _dbFactory.CreateDbContextAsync();

    var now = DateTime.UtcNow;
    var from = new DateOnly(now.Year, now.Month, 1).AddMonths(-(months - 1));

    // اجلب كل البيانات مرة واحدة (أسرع من queries متعددة)
    var hires = await _db.Users
        .Where(u => u.HireDate >= from)
        .Select(u => new { u.HireDate })
        .ToListAsync();

    var terminations = await _db.Users
        .Where(u => u.FinishJob != null
                 && u.FinishJob >= from)
        .Select(u => new { u.FinishJob })
        .ToListAsync();

    var result = new List<HiringTrendPoint>();

    for (int i = 0; i < months; i++)
    {
        var monthStart = from.AddMonths(i);
        var monthEnd = monthStart.AddMonths(1);

        var hiredCount = hires.Count(h =>
            h.HireDate >= monthStart && h.HireDate <= monthEnd);

        var leftCount = terminations.Count(t =>
            t.FinishJob >= monthStart && t.FinishJob <= monthEnd);

        result.Add(new HiringTrendPoint
        {
            Year = monthStart.Year,
            Month = monthStart.Month,
            MonthLabel = monthStart.ToString("MMM yyyy", 
                System.Globalization.CultureInfo.GetCultureInfo("ar-EG")),
            Hired = hiredCount,
            Left = leftCount,
        });
    }

    return result;
}

/// <summary>
/// تفاصيل الموظفين المعيّنين في فترة (للـ drill-down)
/// </summary>
public async Task<List<HiredEmployeeItem>> GetHiredEmployeesAsync(
    DateOnly from, DateOnly to)
{
    using var _db = await _dbFactory.CreateDbContextAsync();

    return await _db.Users
        .Include(u => u.Department)
        .Include(u => u.Branch)
        .Include(u => u.JobTitle)
        .Where(u => u.HireDate >= from && u.HireDate <= to)
        .Select(u => new HiredEmployeeItem
        {
            UserId = u.Id,
            FullName = u.FullName,
            Code = u.Code,
            JobTitle = u.JobTitle != null ? u.JobTitle.Name : null,
            Department = u.Department != null ? u.Department.Name : null,
            Branch = u.Branch != null ? u.Branch.Name : null,
            HireDate = u.HireDate,
        })
        .OrderByDescending(u => u.HireDate)
        .ToListAsync();
}

/// <summary>
/// تفاصيل الموظفين اللي مشيوا في فترة
/// </summary>
public async Task<List<HiredEmployeeItem>> GetTerminatedEmployeesAsync(
    DateOnly from, DateOnly to)
{
    using var _db = await _dbFactory.CreateDbContextAsync();

    return await _db.Users
        .Include(u => u.Department)
        .Include(u => u.Branch)
        .Include(u => u.JobTitle)
        .Where(u => u.IsArchived
                 && u.HireDate != null
                 && u.FinishJob >= from
                 && u.FinishJob <= to)
        .Select(u => new HiredEmployeeItem
        {
            UserId = u.Id,
            FullName = u.FullName,
            Code = u.Code,
            JobTitle = u.JobTitle != null ? u.JobTitle.Name : null,
            Department = u.Department != null ? u.Department.Name : null,
            Branch = u.Branch != null ? u.Branch.Name : null,
            HireDate = u.FinishJob.Value, // نستخدم نفس الحقل للعرض
        })
        .OrderByDescending(u => u.HireDate)
        .ToListAsync();
}

        public async Task<DashboardTreeNode> GetCompanyTreeAsync()
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var companyName = await _db.Settings.Select(s => s.CompanyName).FirstOrDefaultAsync() ?? "الشركة";
            var sectors = await _db.Degrees.Include(d => d.Users).OrderBy(d => d.Name).ToListAsync();
            var branches = await _db.Branches.Include(b => b.Users).OrderBy(b => b.Name).ToListAsync();
            var depts = await _db.Departments.Include(d => d.Users).OrderBy(d => d.Name).ToListAsync();

            var root = new DashboardTreeNode
            {
                Name = companyName,
                Type = "الشركة",
                ChildrenType = "قطاعات",
                TotalEmployees = sectors.Sum(s => s.Users.Count(u => u.InDuty && !u.IsArchived)),
                TotalBranches = branches.Count,
                TotalDeparts = depts.Count,
                TotalChildren = sectors.Count,
                Children = sectors.Select(s => new DashboardTreeNode
                {
                    Id = s.Id,
                    Name = s.Name,
                    Type = "قطاع",
                    ChildrenType = "فروع",
                    TotalEmployees = s.Users.Count(u => u.InDuty && !u.IsArchived),
                    TotalChildren = branches.Count(b => b.Users.Any(u => u.InDuty && u.DegreeId == s.Id)),
                    Children = branches.Where(b => b.Users.Any(u => u.InDuty && u.DegreeId == s.Id)).Select(b => new DashboardTreeNode
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Type = "فرع",
                        ChildrenType = "إدارات",
                        TotalEmployees = b.Users.Count(u => u.InDuty && !u.IsArchived && u.DegreeId == s.Id),
                        TotalChildren = depts.Count(d => d.Users.Any(u => u.InDuty && u.DegreeId == s.Id && u.BranchId == b.Id)),
                        Children = depts.Where(d => d.Users.Any(u => u.InDuty && u.DegreeId == s.Id && u.BranchId == b.Id)).Select(d => new DashboardTreeNode
                        {
                            Id = d.Id,
                            Name = d.Name,
                            Type = "إدارة",
                            TotalEmployees = d.Users.Count(u => u.InDuty && !u.IsArchived && u.DegreeId == s.Id && u.BranchId == b.Id && u.DepartmentId == d.Id),
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            return root;
        }

        // Add these methods to DashboardService
public async Task<ManagerDashboardStats> GetManagerDashboardStatsAsync(int managerId)
{
    using var _db = await _dbFactory.CreateDbContextAsync();
    
    var stats = new ManagerDashboardStats();
    var today = DateTime.Today;
    
    var teamMembers = await _db.Users
        .Where(u => u.InDuty && u.ManagerId == managerId && !u.IsArchived)
        .ToListAsync();
    
    stats.TotalEmployees = teamMembers.Count;
    
    var teamIds = teamMembers.Select(m => m.Id).ToList();
    
    var todayAttendance = await _db.Attendances
        .Where(a => teamIds.Contains(a.UserId) && a.AttendanceDate.Date == today)
        .ToListAsync();
    
    stats.PresentToday = todayAttendance.Count(a => a.CheckInTime.HasValue);
    stats.AbsentToday = todayAttendance.Count(a => a.IsAbsence);
    stats.LateToday = todayAttendance.Count(a => a.Late.HasValue && a.Late.Value > TimeSpan.Zero);
    
    var pendingApprovals = await _db.Leaves
        .CountAsync(l => teamIds.Contains(l.UserId) && l.Status == 1);
    
    pendingApprovals += await _db.Loans
        .CountAsync(l => teamIds.Contains(l.UserId) && l.Status == "Pending");
    
    pendingApprovals += await _db.EmployeePermissions
        .CountAsync(p => teamIds.Contains(p.UserId) && p.Status == "Pending");
    
    stats.PendingApprovals = pendingApprovals;
    
    return stats;
}

public async Task<List<TeamCheckInItem>> GetTeamCheckInsAsync(int managerId)
{
    using var _db = await _dbFactory.CreateDbContextAsync();
    var today = DateTime.Today;
    
    var teamMembers = await _db.Users
        .Where(u => u.InDuty && u.ManagerId == managerId && !u.IsArchived)
        .ToListAsync();
    
    var teamIds = teamMembers.Select(m => m.Id).ToList();
    
    return await _db.Attendances
        .Include(a => a.User)
        .ThenInclude(u => u.Department)
        .Where(a => teamIds.Contains(a.UserId) && a.AttendanceDate.Date == today && a.CheckInTime.HasValue)
        .Select(a => new TeamCheckInItem
        {
            EmployeeName = a.User.FullName,
            DepartmentName = a.User.Department != null ? a.User.Department.Name : "",
            CheckInTime = a.CheckInTime,
            IsLate = a.Late.HasValue && a.Late.Value > TimeSpan.Zero
        })
        .OrderBy(a => a.CheckInTime)
        .ToListAsync();
}

        // ══ بيانات الرسوم البيانية الأربعة ══
        public async Task<DashboardChartsData> GetChartsDataAsync()
        {
            var data = new DashboardChartsData();

            using var _db = await _dbFactory.CreateDbContextAsync();
            data.MaleCount = await _db.Users.Where(u => u.InDuty && !u.IsArchived && u.Gender == 'M').CountAsync();
            data.FemaleCount = await _db.Users.Where(u => u.InDuty && !u.IsArchived && u.Gender == 'F').CountAsync();

            var depts = await _db.Departments.Include(d => d.Users)
                .OrderByDescending(d => d.Users.Count(u => u.InDuty && !u.IsArchived))
                .Take(8).ToListAsync();
            data.DepartmentLabels = depts.Select(d => d.Name).ToList();
            data.DepartmentCounts = depts.Select(d => d.Users.Count(u => !u.IsArchived)).ToList();

            var branches = await _db.Branches.Include(b => b.Users)
                .OrderByDescending(b => b.Users.Count(u => u.InDuty && !u.IsArchived))
                .Take(10).ToListAsync();
            data.BranchLabels = branches.Select(b => b.Name).ToList();
            data.BranchCounts = branches.Select(b => b.Users.Count(u => u.InDuty && !u.IsArchived)).ToList();

            var sectors = await _db.Degrees.Include(d => d.Users)
                .OrderByDescending(d => d.Users.Count(u => u.InDuty && !u.IsArchived))
                .ToListAsync();
            data.SectorLabels = sectors.Select(s => s.Name).ToList();
            data.SectorCounts = sectors.Select(s => s.Users.Count(u => u.InDuty && !u.IsArchived)).ToList();

            return data;
        }

        // ══ التنبيهات ══
        public async Task<List<DashboardAlert>> GetAlertsAsync()
        {
            var alerts = new List<DashboardAlert>();
            using var _db = await _dbFactory.CreateDbContextAsync();

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