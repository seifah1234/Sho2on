using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services;

public class PermissionManagementService
{
    private readonly AppDbContext _db;

    public PermissionManagementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PermissionListItem>> GetRequestsAsync(
        string? status,
        string? search)
    {
        var query = _db.EmployeePermissions
            .Include(x => x.User)
            .Include(x => x.ApprovedBy)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.User!.FullName.Contains(search) ||
                x.User.Code.Contains(search));
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PermissionListItem
            {
                Id = x.Id,
                EmployeeName = x.User!.FullName,
                EmployeeCode = x.User.Code,
                PermissionType = x.PermissionType,
                StartDateTime = x.StartDateTime,
                EndDateTime = x.EndDateTime,
                Duration = x.Duration,
                Status = x.Status,
                Reason = x.Reason,
                ApproverName = x.ApprovedBy != null ? x.ApprovedBy.FullName : ""
            })
            .ToListAsync();
    }

    public async Task ApproveAsync(int permissionId, int currentUserId)
    {
        var permission = await _db.EmployeePermissions
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == permissionId)
            ?? throw new InvalidOperationException("طلب الإذن غير موجود");

        if (permission.Status != PermissionStatus.Pending)
            throw new InvalidOperationException("لا يمكن اعتماد طلب ليس قيد الانتظار");

        if (permission.ApprovedByUserId.HasValue &&
            permission.ApprovedByUserId != currentUserId)
        {
            throw new InvalidOperationException("هذا الطلب موجه لمسؤول اعتماد آخر");
        }

        permission.Status = PermissionStatus.Approved;
        permission.ApprovedDate = DateTime.Now;
        permission.UpdatedAt = DateTime.Now;

        await ApplyToAttendanceAsync(permission);

        await _db.SaveChangesAsync();
    }

    public async Task RejectAsync(
        int permissionId,
        int currentUserId,
        string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new InvalidOperationException("سبب الرفض مطلوب");

        var permission = await _db.EmployeePermissions
            .FirstOrDefaultAsync(x => x.Id == permissionId)
            ?? throw new InvalidOperationException("طلب الإذن غير موجود");

        if (permission.Status != PermissionStatus.Pending)
            throw new InvalidOperationException("لا يمكن رفض طلب ليس قيد الانتظار");

        if (permission.ApprovedByUserId.HasValue &&
            permission.ApprovedByUserId != currentUserId)
        {
            throw new InvalidOperationException("هذا الطلب موجه لمسؤول اعتماد آخر");
        }

        permission.Status = PermissionStatus.Rejected;
        permission.RejectionReason = rejectionReason.Trim();
        permission.ApprovedDate = DateTime.Now;
        permission.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    private async Task ApplyToAttendanceAsync(EmployeePermission permission)
    {
        var date = permission.StartDateTime.Date;

        var attendance = await _db.Attendances.FirstOrDefaultAsync(x =>
            x.UserId == permission.UserId &&
            x.AttendanceDate == date);

        // لو سجل الحضور غير موجود، ننشئه بدون بصمة أو وقت حضور.
        if (attendance is null)
        {
            var employee = await _db.Users.FindAsync(permission.UserId)
                ?? throw new InvalidOperationException("الموظف غير موجود");

            attendance = new Attendance
            {
                UserId = employee.Id,
                AttendanceDate = date,
                ShiftId = employee.ShiftId,
                CheckInBranchId = employee.BranchId,
                CheckOutBranchId = employee.BranchId
            };

            _db.Attendances.Add(attendance);
        }

        // الإذن لا يكتب وقتًا وهميًا ولا يمسح البصمات.
        switch (permission.PermissionType)
        {
            case PermissionTypes.LateEntry:
                attendance.ExemptLate = true;
                break;

            case PermissionTypes.EarlyLeave:
                attendance.ExemptEarlyLeave = true;
                break;

            // إذن شخصي/رسمي/طارئ: يسجل كاستثناء للحضور في اليوم.
            default:
                attendance.ExemptLate = true;
                attendance.ExemptEarlyLeave = true;
                break;
        }
    }

    public class PermissionListItem
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public string PermissionType { get; set; } = "";
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public double Duration { get; set; }
        public string Status { get; set; } = "";
        public string Reason { get; set; } = "";
        public string ApproverName { get; set; } = "";
    }
}