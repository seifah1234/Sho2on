using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services;

public class MissionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly CurrentUserService _currentUserService;
    private readonly NotificationCenterService _notify;

    public MissionService(IDbContextFactory<AppDbContext> dbFactory, CurrentUserService currentUserService, NotificationCenterService notify)
    {
        _dbFactory = dbFactory;
        _currentUserService = currentUserService;
        _notify = notify;
    }

    public async Task<List<MissionListItem>> GetRequestsAsync(
    int? employeeId = null,
    string? employeeName = null,
    string? employeeCode = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    string? status = null)
    {
        var _context = await _dbFactory.CreateDbContextAsync();
        var query = _context.Procedures
            .Include(p => p.User)
                .ThenInclude(u => u.Department)
            .Include(p => p.User)
                .ThenInclude(u => u.JobTitle)
            .Include(p => p.ApprovedBy)
            .Include(p => p.Branch)
            .Where(p => p.Type == 1)
            .AsNoTracking()
            .AsQueryable();

        var user = await _currentUserService.GetCurrentUserAsync();


        if (user != null && user.JobTitle != null && (!user.JobTitle.IsHR.HasValue || !user.JobTitle.IsHR.Value))
        {
            query = query.Where(x => x.ApprovedByUserId == user.Id || x.UserId == user.Id);
        }


        if (employeeId.HasValue && employeeId.Value > 0)
        {
            query = query.Where(p => p.UserId == employeeId.Value);
        }

        if (!string.IsNullOrEmpty(employeeCode))
        {
            query = query.Where(p => p.User != null && p.User.Code.Contains(employeeCode));
        }

        if (!string.IsNullOrEmpty(employeeName))
        {
            query = query.Where(p => p.User != null && p.User.FullName.Contains(employeeName));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.StartDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.EndDate <= toDate.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new MissionListItem
            {
                Id = p.Id,
                EmployeeId = p.UserId.HasValue ? p.UserId.Value : 0,
                EmployeeName = p.User != null ? p.User.FullName : "غير معروف",
                EmployeeCode = p.User != null ? p.User.Code : "",
                EmployeeDepartment = p.User != null && p.User.Department != null ? p.User.Department.Name : "غير معروف",
                EmployeeJobTitle = p.User != null && p.User.JobTitle != null ? p.User.JobTitle.Name : "غير معروف",
                StartDateTime = p.StartDate.HasValue ? p.StartDate.Value : DateTime.MinValue,
                EndDateTime = p.EndDate.HasValue ? p.EndDate.Value : DateTime.MinValue,
                Duration = p.StartDate.HasValue && p.EndDate.HasValue ? Math.Round((p.EndDate.Value - p.StartDate.Value).TotalHours, 2) : 0,
                Status = p.Status,
                StatusText = GetStatusText(p.Status),
                CreatedAt = p.CreatedAt.Value,
                ApprovedByName = p.ApprovedBy != null ? p.ApprovedBy.FullName : "لم تتم الموافقة بعد",
                ApprovedDate = p.ApprovedDate,
                Notes = p.Notes,
                BranchName = p.Branch != null ? p.Branch.Name : ""
            })
            .ToListAsync();
    }

    public async Task<List<MissionListItem>> GetMyRequestsAsync(int userId)
    {
        var _context = await _dbFactory.CreateDbContextAsync();
        var query = _context.Procedures
            .Include(p => p.User)
                .ThenInclude(u => u.Department)
            .Include(p => p.User)
                .ThenInclude(u => u.JobTitle)
            .Include(p => p.ApprovedBy)
            .Include(p => p.Branch)
            .Where(p => p.Type == 1 && p.UserId == userId)
            .AsNoTracking()
            .AsQueryable();

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new MissionListItem
            {
                Id = p.Id,
                EmployeeId = p.UserId.Value,
                EmployeeName = p.User != null ? p.User.FullName : "غير معروف",
                EmployeeCode = p.User != null ? p.User.Code : "",
                EmployeeDepartment = p.User != null && p.User.Department != null ? p.User.Department.Name : "غير معروف",
                EmployeeJobTitle = p.User != null && p.User.JobTitle != null ? p.User.JobTitle.Name : "غير معروف",
                StartDateTime = p.StartDate.Value,
                EndDateTime = p.EndDate.Value,
                Duration = Math.Round((p.EndDate.Value - p.StartDate.Value).TotalHours, 2),
                Status = p.Status,
                StatusText = GetStatusText(p.Status),
                CreatedAt = p.CreatedAt.Value,
                ApprovedByName = p.ApprovedBy != null ? p.ApprovedBy.FullName : "لم تتم الموافقة بعد",
                ApprovedDate = p.ApprovedDate,
                Notes = p.Notes,
                BranchName = p.Branch != null ? p.Branch.Name : ""
            })
            .ToListAsync();
    }

    public async Task<bool> CreateRequestAsync(MissionRequestFormModel model)
    {
        if (!model.StartDate.HasValue || !model.EndDate.HasValue)
            throw new InvalidOperationException("يرجى تحديد تاريخ البداية والنهاية");

        if (model.StartDate.Value >= model.EndDate.Value)
            throw new InvalidOperationException("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");

        if (model.StartDate.Value < DateTime.Today)
            throw new InvalidOperationException("لا يمكن تقديم طلب بتاريخ سابق");

        var _context = await _dbFactory.CreateDbContextAsync();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == model.UserId);

        if (user == null)
            throw new InvalidOperationException("الموظف غير موجود");

        if (!model.ApproverId.HasValue)
            throw new InvalidOperationException("يرجى اختيار الموافق على المأمورية");

        var approver = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == model.ApproverId.Value);

        if (approver == null)
            throw new InvalidOperationException("الموافق غير موجود");

        // التحقق من عدم وجود طلب مأمورية متداخل
        var hasConflict = await _context.Procedures
            .AnyAsync(p =>
                p.UserId == model.UserId &&
                p.Type == 1 &&
                p.Status != "Rejected" &&
                p.Status != "Cancelled" &&
                p.StartDate <= model.EndDate.Value &&
                p.EndDate >= model.StartDate.Value);

        if (hasConflict)
            throw new InvalidOperationException("يوجد طلب مأمورية آخر متداخل مع الفترة المحددة");

        var procedure = new Procedure
        {
            UserId = model.UserId,
            Notes = model.Notes ?? "",
            StartDate = model.StartDate.Value,
            EndDate = model.EndDate.Value,
            Type = 1, // مأمورية
            BranchId = user.BranchId,
            Status = "Pending",
            ApprovedByUserId = model.ApproverId.Value,
            CreatedAt = DateTime.Now
        };

        _context.Procedures.Add(procedure);

        var employee = await _context.Users.FindAsync(model.UserId);
        var managers = await _context.Users
            .Where(u => u.Id == employee!.ManagerId)
            .Select(u => u.Id)
            .ToListAsync();

        if (managers.Count > 0)
        {
            await _notify.CreateForApproversAsync(managers,
                "طلب مأمورية جديد",
                $"{employee!.FullName} قدّم طلب مأمورية يحتاج موافقتك",
                "bi-calendar-check",
                "/leaves/missions");
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ApproveAsync(int missionId, int currentUserId)
    {
        var _context = await _dbFactory.CreateDbContextAsync();
        var mission = await _context.Procedures
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == missionId);

        if (mission == null)
            throw new InvalidOperationException("طلب المأمورية غير موجود");

        if (mission.Status != "Pending")
            throw new InvalidOperationException("لا يمكن الموافقة على طلب ليس قيد الانتظار");

        // التحقق من صلاحيات المستخدم
        var currentUser = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == currentUserId);

        if (currentUser == null)
            throw new InvalidOperationException("المستخدم غير موجود");

        bool isManager = currentUser.JobTitle?.IsManager == true;
        bool isHR = currentUser.Department?.IsHR == true;

        // إذا كان المستخدم هو الموافق المحدد في الطلب أو مدير أو HR
        if (mission.ApprovedByUserId != currentUserId && !isManager && !isHR)
            throw new InvalidOperationException("ليس لديك صلاحية الموافقة على هذا الطلب");

        if (isHR)
        {
            mission.Status = "Approved";
        }
        else
        {
            mission.Status = "UnderReview";
        }

        mission.ApprovedDate = DateTime.Now;
        mission.ApprovedByUserId = currentUserId;

        // تحديث سجل الحضور
        await UpdateAttendanceForMission(mission);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAsync(int missionId, int currentUserId)
    {
        var _context = await _dbFactory.CreateDbContextAsync();
        var mission = await _context.Procedures
            .FirstOrDefaultAsync(p => p.Id == missionId);

        if (mission == null)
            throw new InvalidOperationException("طلب المأمورية غير موجود");

        if (mission.Status != "Pending")
            throw new InvalidOperationException("لا يمكن رفض طلب ليس قيد الانتظار");

        // التحقق من صلاحيات المستخدم
        var currentUser = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == currentUserId);

        if (currentUser == null)
            throw new InvalidOperationException("المستخدم غير موجود");

        bool isManager = currentUser.JobTitle?.IsManager == true;
        bool isHR = currentUser.Department?.IsHR == true;

        if (mission.ApprovedByUserId != currentUserId && !isManager && !isHR)
            throw new InvalidOperationException("ليس لديك صلاحية رفض هذا الطلب");

        mission.Status = "Rejected";
        mission.ApprovedDate = DateTime.Now;
        mission.ApprovedByUserId = currentUserId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserLookup>> SearchEmployeesAsync(string? search)
    {
        var _context = await _dbFactory.CreateDbContextAsync();
        var query = _context.Users
            .Where(u => !u.IsArchived)
            .Include(u => u.Shift)
            .Include(u => u.Manager)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                u.Code.Contains(search));
        }

        return await query
            .OrderBy(u => u.FullName)
            .Take(30)
            .Select(u => new UserLookup
            {
                Id = u.Id,
                Code = u.Code,
                FullName = u.FullName,
                ManagerId = u.ManagerId,
                ShiftStartTime = u.Shift != null ? u.Shift.StartTime : TimeSpan.Zero,
                ShiftEndTime = u.Shift != null ? u.Shift.EndTime : TimeSpan.Zero,
                ExemptEarlyLeave = u.ExemptEarlyLeave,
                ExemptOvertime = u.ExemptOvertime,
                ExemptLate = u.ExemptLate,
                ExemptEarlyEnter = u.ExemptEarlyEnter,
                BranchId = u.BranchId
            })
            .ToListAsync();
    }

    public async Task<List<UserLookup>> GetApproversAsync()
    {
        var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.Users
            .Include(u => u.JobTitle)
            .Where(u => u.JobTitle != null && u.JobTitle.IsManager == true)
            .OrderBy(u => u.FullName)
            .Select(u => new UserLookup
            {
                Id = u.Id,
                Code = u.Code,
                FullName = u.FullName
            })
            .ToListAsync();
    }

    public async Task<UserLookup?> GetUserByCodeAsync(string code)
    {
        var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.Users
            .Include(u => u.Shift)
            .Include(u => u.Manager)
            .Where(u => u.Code == code && !u.IsArchived)
            .Select(u => new UserLookup
            {
                Id = u.Id,
                Code = u.Code,
                FullName = u.FullName,
                ManagerId = u.ManagerId,
                ShiftStartTime = u.Shift != null ? u.Shift.StartTime : TimeSpan.Zero,
                ShiftEndTime = u.Shift != null ? u.Shift.EndTime : TimeSpan.Zero,
                ExemptEarlyLeave = u.ExemptEarlyLeave,
                ExemptOvertime = u.ExemptOvertime,
                ExemptLate = u.ExemptLate,
                ExemptEarlyEnter = u.ExemptEarlyEnter,
                BranchId = u.BranchId,
                ManagerName = u.Manager != null ? u.Manager.FullName : ""
            })
            .FirstOrDefaultAsync();
    }

    private async Task UpdateAttendanceForMission(Procedure mission)
    {
        var _context = await _dbFactory.CreateDbContextAsync();
        var user = await _context.Users
            .Include(u => u.Shift)
            .FirstOrDefaultAsync(u => u.Id == mission.UserId);

        if (user == null || user.Shift == null)
            return;

        DateTime? clockIn = mission.StartDate;
        DateTime? clockOut = mission.EndDate;

        TimeSpan late = TimeSpan.Zero;
        if (user.ExemptLate && clockIn != null && clockIn.Value.TimeOfDay > user.Shift.StartTime)
        {
            late = TimeSpan.FromMinutes((clockIn.Value.TimeOfDay - user.Shift.StartTime).TotalMinutes);
        }

        TimeSpan early = TimeSpan.Zero;
        if (user.ExemptEarlyLeave && clockOut != null && clockOut.Value.TimeOfDay < user.Shift.EndTime)
        {
            early = TimeSpan.FromMinutes((user.Shift.EndTime - clockOut.Value.TimeOfDay).TotalMinutes);
        }

        TimeSpan inEarly = TimeSpan.Zero;
        if (user.ExemptEarlyEnter && clockIn != null && clockIn.Value.TimeOfDay < user.Shift.StartTime)
        {
            inEarly = TimeSpan.FromMinutes((user.Shift.StartTime - clockIn.Value.TimeOfDay).TotalMinutes);
        }

        TimeSpan overtime = TimeSpan.Zero;
        if (user.ExemptOvertime && clockOut != null && clockOut.Value.TimeOfDay > user.Shift.EndTime)
        {
            overtime = TimeSpan.FromMinutes((clockOut.Value.TimeOfDay - user.Shift.EndTime).TotalMinutes);
        }

        TimeSpan attendTime = TimeSpan.Zero;
        if (clockIn != null && clockOut != null)
        {
            TimeSpan clockInTime = clockIn.Value.TimeOfDay;
            TimeSpan clockOutTime = clockOut.Value.TimeOfDay;

            if (clockOutTime < clockInTime)
            {
                clockOutTime += TimeSpan.FromDays(1);
            }

            attendTime = clockOutTime - clockInTime;
        }

        var dayDate = mission.EndDate.Value.Date;
        var attendance = await _context.Attendances
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.AttendanceDate == dayDate);

        if (attendance == null)
        {
            attendance = new Attendance
            {
                UserId = user.Id,
                AttendanceDate = dayDate,
                ShiftId = user.ShiftId,
                CheckInBranchId = user.BranchId,
                CheckOutBranchId = user.BranchId
            };
            _context.Attendances.Add(attendance);
        }

        attendance.CheckInTime = clockIn;
        attendance.CheckOutTime = clockOut;
        attendance.Late = late;
        attendance.EarlyLeave = early;
        attendance.Overtime = overtime;
        attendance.TotalWorkHours = attendTime;
        attendance.EarlyEnter = inEarly;
        attendance.ExemptLate = user.ExemptLate;
        attendance.ExemptEarlyEnter = user.ExemptEarlyEnter;
        attendance.ExemptEarlyLeave = user.ExemptEarlyLeave;
        attendance.ExemptOvertime = user.ExemptOvertime;

        await _context.SaveChangesAsync();
    }

    private static string GetStatusText(string status)
    {
        return status switch
        {
            "UnderReview" => "تحت المراجعة",
            "Pending" => "قيد الانتظار",
            "Approved" => "موافق عليه",
            "Rejected" => "مرفوض",
            "Draft" => "مسودة",
            _ => status
        };
    }

    public class UserLookup
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string FullName { get; set; } = "";
        public int? ManagerId { get; set; }
        public string ManagerName { get; set; } = "";
        public TimeSpan ShiftStartTime { get; set; }
        public TimeSpan ShiftEndTime { get; set; }
        public bool ExemptEarlyLeave { get; set; }
        public bool ExemptOvertime { get; set; }
        public bool ExemptLate { get; set; }
        public bool ExemptEarlyEnter { get; set; }
        public int? BranchId { get; set; }
    }
}