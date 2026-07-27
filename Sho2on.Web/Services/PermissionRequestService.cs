using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services;

public class PermissionRequestService
{
    private readonly AppDbContext _db;

    public PermissionRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmployeeItem>> SearchEmployeesAsync(string? search)
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
            .Select(x => new EmployeeItem
            {
                Id = x.Id,
                FullName = x.FullName,
                Code = x.Code,
                ManagerId = x.ManagerId,
                BranchId = x.BranchId
            })
            .ToListAsync();
    }

    public async Task CreateAsync(PermissionRequestFormModel model)
    {
        if (!model.Date.HasValue ||
    !TimeOnly.TryParse(model.StartTime, out var startTime) ||
    !TimeOnly.TryParse(model.EndTime, out var endTime))
        {
            throw new InvalidOperationException("أدخل تاريخ ووقت الإذن بصورة صحيحة");
        }

        if (endTime <= startTime)
            throw new InvalidOperationException("وقت النهاية يجب أن يكون بعد وقت البداية");

        if (model.Date.Value.Date < DateTime.Today)
            throw new InvalidOperationException("لا يمكن تقديم إذن بتاريخ سابق");

        var employee = await _db.Users
            .FirstOrDefaultAsync(x => x.Id == model.UserId && !x.IsArchived)
            ?? throw new InvalidOperationException("الموظف غير موجود");

        var approverExists = await _db.Users.AnyAsync(x =>
            x.Id == model.ApproverId &&
            !x.IsArchived);

        if (!approverExists)
            throw new InvalidOperationException("المسؤول عن الاعتماد غير موجود");

        var startDateTime = model.Date.Value.Date
            .Add(startTime.ToTimeSpan());

        var endDateTime = model.Date.Value.Date
            .Add(endTime.ToTimeSpan());

        var hasConflict = await _db.EmployeePermissions.AnyAsync(x =>
            x.UserId == model.UserId &&
            (x.Status == PermissionStatus.Pending || x.Status == PermissionStatus.UnderReview || x.Status == PermissionStatus.Approved) &&
            x.StartDateTime < endDateTime &&
            x.EndDateTime > startDateTime);

        if (hasConflict)
            throw new InvalidOperationException("يوجد إذن آخر متداخل مع الوقت المحدد");

        _db.EmployeePermissions.Add(new EmployeePermission
        {
            UserId = model.UserId,
            PermissionType = model.PermissionType,
            StartDateTime = startDateTime,
            EndDateTime = endDateTime,
            Duration = (endDateTime - startDateTime).TotalHours,
            Reason = model.Reason.Trim(),
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            Status = PermissionStatus.Pending,

            // في الطلب الجديد، هذا هو المدير المكلّف بالاعتماد.
            ApprovedByUserId = model.ApproverId,

            BranchId = employee.BranchId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();
    }

    public class EmployeeItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Code { get; set; } = "";
        public int? ManagerId { get; set; }
        public int BranchId { get; set; }
    }
}