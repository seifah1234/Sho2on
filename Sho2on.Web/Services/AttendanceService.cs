using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class AttendanceService
    {
        private readonly AppDbContext _db;
        public AttendanceService(AppDbContext db) => _db = db;

        public async Task<PagedResult<AttendanceListItem>> GetPagedListAsync(
            DateOnly date, int? branchId, string? search, int page, int pageSize)
        {
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);

            var query = _db.Attendances
                .Include(a => a.User)
                .Include(a => a.CheckInBranch)
                .Where(a => a.AttendanceDate >= dayStart && a.AttendanceDate < dayEnd)
                .AsQueryable();

            if (branchId.HasValue)
                query = query.Where(a => a.CheckInBranchId == branchId.Value || (a.User != null && a.User.BranchId == branchId.Value));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.User!.FullName.Contains(search) || a.User!.Code.Contains(search));

            var totalCount = await query.CountAsync();

            var raw = await query
                .OrderBy(a => a.User!.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.User!.FullName,
                    a.User.Code,
                    BranchName = a.CheckInBranch != null ? a.CheckInBranch.Name : a.User.Branch.Name,
                    a.AttendanceDate,
                    a.CheckInTime,
                    a.CheckOutTime,
                    a.Late,
                    a.IsAbsence,
                    a.IsHoliday,
                    a.LeaveId
                })
                .ToListAsync();

            var items = raw.Select(a => new AttendanceListItem
            {
                Id = a.Id,
                EmployeeName = a.FullName,
                EmployeeCode = a.Code,
                BranchName = a.BranchName,
                AttendanceDate = a.AttendanceDate,
                CheckInTime = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                Late = a.Late,
                Status = a.IsAbsence ? "غائب"
                       : a.LeaveId.HasValue ? "إجازة"
                       : a.IsHoliday ? "عطلة"
                       : a.CheckInTime.HasValue ? "حاضر"
                       : "لم يسجل بعد"
            }).ToList();

            return new PagedResult<AttendanceListItem>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}