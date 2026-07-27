using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services;

public class LeaveRequestService
{
    private readonly AppDbContext _db;

    public LeaveRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmployeeLookup>> SearchEmployeesAsync(string? search)
    {
        var query = _db.Users
            .Where(x => !x.IsArchived)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.FullName.Contains(search) ||
                x.Code.Contains(search));
        }

        return await query
            .OrderBy(x => x.FullName)
            .Take(30)
            .Select(x => new EmployeeLookup
            {
                Id = x.Id,
                Code = x.Code,
                FullName = x.FullName,
                ManagerId = x.ManagerId
            })
            .ToListAsync();
    }

    public async Task<List<LeaveTypeLookup>> GetActiveLeaveTypesAsync()
    {
        return await _db.LeaveTypes
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .Select(x => new LeaveTypeLookup
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                DeductFromBalance = x.DeductFromBalance,
                RequiresApproval = x.RequiresApproval,
                MaxConsecutiveDays = x.MaxConsecutiveDays
            })
            .ToListAsync();
    }

    public async Task<BalanceInfo> GetBalanceAsync(int userId, int leaveTypeId)
    {
        var leaveType = await _db.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == leaveTypeId)
            ?? throw new InvalidOperationException("نوع الإجازة غير موجود");

        var savedBalance = await _db.LeaveBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.LeaveTypeId == leaveTypeId);

        var used = await _db.Leaves
            .Where(x =>
                x.UserId == userId &&
                x.LeaveTypeId == leaveTypeId &&
                x.Status == 2 &&
                !x.IsCancelled)
            .SumAsync(x => (int?)x.Duration) ?? 0;

        var total = savedBalance?.TotalBalance ?? leaveType.DefaultBalance;

        return new BalanceInfo
        {
            Total = total,
            Used = used,
            Remaining = total - used
        };
    }

    public async Task CreateAsync(LeaveRequestFormModel model)
    {
        if (!model.StartDate.HasValue || !model.EndDate.HasValue)
            throw new InvalidOperationException("حدد تاريخ بداية ونهاية الإجازة");

        var startDate = model.StartDate.Value.Date;
        var endDate = model.EndDate.Value.Date;

        if (startDate < DateTime.Today)
            throw new InvalidOperationException("لا يمكن تقديم طلب إجازة بتاريخ سابق");

        if (endDate < startDate)
            throw new InvalidOperationException("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");

        if (model.ReplacementUserId == model.UserId)
            throw new InvalidOperationException("لا يمكن اختيار الموظف نفسه كبديل");

        var employeeExists = await _db.Users.AnyAsync(x =>
            x.Id == model.UserId && !x.IsArchived);

        if (!employeeExists)
            throw new InvalidOperationException("الموظف غير موجود");

        var leaveType = await _db.LeaveTypes.FirstOrDefaultAsync(x =>
            x.Id == model.LeaveTypeId && x.IsActive);

        if (leaveType is null)
            throw new InvalidOperationException("نوع الإجازة غير موجود أو غير نشط");

        var duration = (endDate - startDate).Days + 1;

        if (leaveType.MaxConsecutiveDays.HasValue &&
            duration > leaveType.MaxConsecutiveDays.Value)
        {
            throw new InvalidOperationException(
                $"الحد الأقصى لهذا النوع هو {leaveType.MaxConsecutiveDays} يوم");
        }

        var hasConflict = await _db.Leaves.AnyAsync(x =>
            x.UserId == model.UserId &&
            !x.IsCancelled &&
            (x.Status == 1 || x.Status == 2) &&
            x.StartDate <= endDate &&
            x.EndDate >= startDate);

        if (hasConflict)
            throw new InvalidOperationException(
                "يوجد طلب إجازة آخر متداخل مع الفترة المحددة");

        if (leaveType.DeductFromBalance)
        {
            var balance = await GetBalanceAsync(model.UserId, model.LeaveTypeId);

            if (duration > balance.Remaining)
            {
                throw new InvalidOperationException(
                    $"الرصيد المتبقي غير كافٍ. المتبقي: {balance.Remaining} يوم");
            }
        }

        if (leaveType.RequiresApproval && !model.ApproverId.HasValue)
            throw new InvalidOperationException("اختر المسؤول عن اعتماد الإجازة");

        var status = leaveType.RequiresApproval ? 1 : 2;

        var leave = new Leave
        {
            UserId = model.UserId,
            LeaveTypeId = model.LeaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            Duration = duration,
            Reason = model.Reason.Trim(),
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            ReplacementUserId = model.ReplacementUserId,
            ApprovedBy = model.ApproverId,
            RequestDate = DateTime.Now,
            Status = status,

            // لو النوع لا يحتاج اعتماد، يعتبر معتمدًا تلقائيًا
            ApprovalDate = status == 2 ? DateTime.Now : null
        };

        _db.Leaves.Add(leave);

        // تحديث UsedBalance هنا فقط في حالة الاعتماد التلقائي.
        if (status == 2 && leaveType.DeductFromBalance)
        {
            var balance = await _db.LeaveBalances.FirstOrDefaultAsync(x =>
                x.UserId == model.UserId &&
                x.LeaveTypeId == model.LeaveTypeId);

            if (balance is null)
            {
                _db.LeaveBalances.Add(new LeaveBalance
                {
                    UserId = model.UserId,
                    LeaveTypeId = model.LeaveTypeId,
                    TotalBalance = leaveType.DefaultBalance,
                    UsedBalance = duration,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }
            else
            {
                balance.UsedBalance += duration;
                balance.UpdatedAt = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
    }

    public class EmployeeLookup
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string FullName { get; set; } = "";
        public int? ManagerId { get; set; }
    }

    public class LeaveTypeLookup
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public bool DeductFromBalance { get; set; }
        public bool RequiresApproval { get; set; }
        public int? MaxConsecutiveDays { get; set; }
    }

    public class BalanceInfo
    {
        public int Total { get; set; }
        public int Used { get; set; }
        public int Remaining { get; set; }
    }
}