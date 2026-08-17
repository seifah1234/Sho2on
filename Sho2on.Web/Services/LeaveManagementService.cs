using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services;

public class LeaveManagementService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly CurrentUserService _currentUserService;
    private readonly NotificationCenterService _notify;

    public LeaveManagementService(IDbContextFactory<AppDbContext> dbFactory, CurrentUserService currentUserService, NotificationCenterService notify)
    {
        _dbFactory = dbFactory;
        _currentUserService = currentUserService;
        _notify = notify;
    }

    public async Task<List<LeaveListItem>> GetRequestsByEmployeeAsync(int employeeId)
    {
        using var _db = await _dbFactory.CreateDbContextAsync();

        return await _db.Leaves
            .Include(l => l.User)
            .Include(l => l.LeaveType)
            .Include(l => l.Approver)
            .Where(l => l.UserId == employeeId)
            .OrderByDescending(l => l.RequestDate)
            .Select(l => new LeaveListItem
            {
                Id = l.Id,
                EmployeeName = l.User.FullName,
                EmployeeCode = l.User.Code ?? "",
                LeaveTypeName = l.LeaveType.Name,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Duration = l.Duration,
                Status = (int)l.Status,
                ApproverName = l.Approver != null ? l.Approver.FullName : ""
            })
            .ToListAsync();
    }

    public async Task<LeaveListItem?> GetLeaveDetailsAsync(int leaveId)
    {
        using var _db = await _dbFactory.CreateDbContextAsync();

        return await _db.Leaves
            .Include(l => l.User)
            .Include(l => l.LeaveType)
            .Include(l => l.Approver)
            .Where(l => l.Id == leaveId)
            .Select(l => new LeaveListItem
            {
                Id = l.Id,
                EmployeeName = l.User.FullName,
                EmployeeCode = l.User.Code ?? "",
                LeaveTypeName = l.LeaveType.Name,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Duration = l.Duration,
                Status = (int)l.Status,
                ApproverName = l.Approver != null ? l.Approver.FullName : "",
                Notes = l.Notes,
                Reason = l.Reason,
                RequestDate = l.RequestDate
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<LeaveListItem>> GetRequestsAsync(
        int status = -1,
        string? search = null)
    {

        using var _db = await _dbFactory.CreateDbContextAsync();
        var query = _db.Leaves
            .Include(x => x.User)
            .Include(x => x.LeaveType)
            .Include(x => x.Approver)
            .AsNoTracking()
            .AsQueryable();

        var user = await _currentUserService.GetCurrentUserAsync();


        if (user != null && user.JobTitle != null && (!user.JobTitle.IsHR.HasValue || !user.JobTitle.IsHR.Value))
        {
            query = query.Where(x => x.ApprovedBy == user.Id || x.UserId == user.Id);
        }

        if (status >= 0)
            query = query.Where(x => x.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.User.FullName.Contains(search) ||
                x.User.Code.Contains(search) ||
                x.LeaveType.Name.Contains(search));
        }

        return await query
            .OrderByDescending(x => x.RequestDate)
            .Select(x => new LeaveListItem
            {
                Id = x.Id,
                EmployeeName = x.User.FullName,
                EmployeeCode = x.User.Code,
                LeaveTypeName = x.LeaveType.Name,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Duration = x.Duration,
                Status = x.Status,
                Reason = x.Reason,
                RequestDate = x.RequestDate,
                ApproverName = x.Approver != null ? x.Approver.FullName : ""
            })
            .ToListAsync();
    }

    public async Task ApproveAsync(int leaveId, int currentUserId)
    {
            using var _db = await _dbFactory.CreateDbContextAsync();
        var leave = await _db.Leaves
            .Include(x => x.User)
            .ThenInclude(x => x.WeekHoliday)
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == leaveId)
            ?? throw new InvalidOperationException("طلب الإجازة غير موجود");

        if (leave.Status != 1)
            throw new InvalidOperationException("لا يمكن اعتماد طلب ليس قيد الانتظار");

        if (leave.ApprovedBy.HasValue && leave.ApprovedBy != currentUserId)
            throw new InvalidOperationException("هذا الطلب موجه لمسؤول اعتماد آخر");

        var hasConflict = await _db.Leaves.AnyAsync(x =>
            x.Id != leave.Id &&
            x.UserId == leave.UserId &&
            x.Status == 2 &&
            !x.IsCancelled &&
            x.StartDate <= leave.EndDate &&
            x.EndDate >= leave.StartDate);

        if (hasConflict)
            throw new InvalidOperationException("توجد إجازة معتمدة متداخلة مع هذه الفترة");

        if (leave.LeaveType.DeductFromBalance)
        {
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(x =>
                    x.UserId == leave.UserId &&
                    x.LeaveTypeId == leave.LeaveTypeId);

            var totalBalance = balance?.TotalBalance ?? leave.LeaveType.DefaultBalance;

            var usedBalance = await _db.Leaves
                .Where(x =>
                    x.Id != leave.Id &&
                    x.UserId == leave.UserId &&
                    x.LeaveTypeId == leave.LeaveTypeId &&
                    x.Status == 2 &&
                    !x.IsCancelled)
                .SumAsync(x => (int?)x.Duration) ?? 0;

            if (usedBalance + leave.Duration > totalBalance)
                throw new InvalidOperationException("رصيد الموظف لم يعد كافيًا لاعتماد هذا الطلب");

            if (balance is null)
            {
                balance = new LeaveBalance
                {
                    UserId = leave.UserId,
                    LeaveTypeId = leave.LeaveTypeId,
                    TotalBalance = totalBalance,
                    UsedBalance = leave.Duration,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _db.LeaveBalances.Add(balance);
            }
            else
            {
                balance.UsedBalance = usedBalance + leave.Duration;
                balance.UpdatedAt = DateTime.Now;
            }
        }

        leave.Status = 2;
        leave.ApprovalDate = DateTime.Now;

        await ApplyLeaveToAttendanceAsync(leave);

        var manager = await _db.Users.FindAsync(currentUserId);


        if (manager != null)
        {
            await _notify.CreateAsync(leave.UserId,
                "مراجعة طلب إجازة",
                $"{manager.FullName} تم الموافقة على طلب الإجازة من",
                "bi-calendar-check",
                "/leaves");
        }

        await _db.SaveChangesAsync();
    }

    public async Task RejectAsync(int leaveId, int currentUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("سبب الرفض مطلوب");

            using var _db = await _dbFactory.CreateDbContextAsync();
        var leave = await _db.Leaves.FindAsync(leaveId)
            ?? throw new InvalidOperationException("طلب الإجازة غير موجود");

        if (leave.Status != 1)
            throw new InvalidOperationException("لا يمكن رفض طلب ليس قيد الانتظار");

        if (leave.ApprovedBy.HasValue && leave.ApprovedBy != currentUserId)
            throw new InvalidOperationException("هذا الطلب موجه لمسؤول اعتماد آخر");

        leave.Status = 3;
        leave.RejectionReason = reason.Trim();
        leave.ApprovalDate = DateTime.Now;

        var manager = await _db.Users.FindAsync(currentUserId);

        if (manager != null)
        {
            await _notify.CreateAsync(leave.UserId,
                "مراجعة طلب إجازة",
                $"{manager.FullName} تم رفض طلب الإجازة من",
                "bi-calendar-check",
                "/leaves");
        }

        await _db.SaveChangesAsync();
    }

    public async Task CancelAsync(int leaveId)
    {
            using var _db = await _dbFactory.CreateDbContextAsync();
        var leave = await _db.Leaves
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == leaveId)
            ?? throw new InvalidOperationException("طلب الإجازة غير موجود");

        if (leave.Status is 3 or 4)
            throw new InvalidOperationException("لا يمكن إلغاء هذا الطلب");

        if (leave.Status == 2 && leave.LeaveType.DeductFromBalance)
        {
            var balance = await _db.LeaveBalances.FirstOrDefaultAsync(x =>
                x.UserId == leave.UserId &&
                x.LeaveTypeId == leave.LeaveTypeId);

            if (balance is not null)
            {
                balance.UsedBalance = Math.Max(0, balance.UsedBalance - leave.Duration);
                balance.UpdatedAt = DateTime.Now;
            }
        }

        // نحذف فقط سجلات الحضور التي أنشأتها هذه الإجازة.
        var leaveAttendances = await _db.Attendances
            .Where(x => x.LeaveId == leave.Id)
            .ToListAsync();

        _db.Attendances.RemoveRange(leaveAttendances);

        leave.Status = 4;
        leave.IsCancelled = true;
        leave.CancelledDate = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    private async Task ApplyLeaveToAttendanceAsync(Leave leave)
    {
        for (var date = leave.StartDate.Date;
             date <= leave.EndDate.Date;
             date = date.AddDays(1))
        {
            if (IsWeeklyRest(date, leave.User.WeekHoliday))
                continue;

            using var _db = await _dbFactory.CreateDbContextAsync();
            var attendance = await _db.Attendances.FirstOrDefaultAsync(x =>
                x.UserId == leave.UserId &&
                x.AttendanceDate == date);

            // لا نمسح حضورًا فعليًا موجودًا.
            if (attendance is not null &&
                (attendance.CheckInTime.HasValue || attendance.CheckOutTime.HasValue))
            {
                throw new InvalidOperationException(
                    $"يوجد حضور فعلي للموظف بتاريخ {date:yyyy/MM/dd}");
            }

            if (attendance is null)
            {
                attendance = new Attendance
                {
                    UserId = leave.UserId,
                    AttendanceDate = date,
                    ShiftId = leave.User.ShiftId,
                    CheckInBranchId = leave.User.BranchId,
                    CheckOutBranchId = leave.User.BranchId
                };

                _db.Attendances.Add(attendance);
            }

            attendance.LeaveId = leave.Id;
            attendance.IsHoliday = true;
            attendance.IsAbsence = false;
            attendance.CheckInTime = null;
            attendance.CheckOutTime = null;
            attendance.Late = null;
            attendance.EarlyLeave = null;
            attendance.EarlyEnter = null;
            attendance.Overtime = null;
            attendance.TotalWorkHours = null;
        }
    }

    private static bool IsWeeklyRest(DateTime date, WeekHoliday? holiday)
    {
        if (holiday is null)
            return false;

        return date.DayOfWeek switch
        {
            DayOfWeek.Saturday => holiday.Day1,
            DayOfWeek.Sunday => holiday.Day2,
            DayOfWeek.Monday => holiday.Day3,
            DayOfWeek.Tuesday => holiday.Day4,
            DayOfWeek.Wednesday => holiday.Day5,
            DayOfWeek.Thursday => holiday.Day6,
            DayOfWeek.Friday => holiday.Day7,
            _ => false
        };
    }

    public class LeaveListItem
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public string LeaveTypeName { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Duration { get; set; }
        public int Status { get; set; }
        public string Reason { get; set; } = "";
        public DateTime RequestDate { get; set; }
        public string ApproverName { get; set; } = "";
    }
}