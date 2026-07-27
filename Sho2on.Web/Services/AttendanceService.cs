using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Web.Models;
using Sho2on.Database.Models;

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

        public async Task<EmployeeMonthlyReportResult?> GetEmployeeMonthlyReportAsync(int userId, int month, int year)
        {
            var user = await _db.Users
                .Include(u => u.Branch)
                .Include(u => u.Shift)
                .Include(u => u.WeekHoliday)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            var(startDate, endDate) = GetMonthRange(month, year);
            var startDt = startDate.ToDateTime(TimeOnly.MinValue);
            var endDt = endDate.ToDateTime(TimeOnly.MaxValue);

            var attendances = await _db.Attendances
                .Include(a => a.Shift)
                .Where(a => a.UserId == userId && a.AttendanceDate >= startDt && a.AttendanceDate <= endDt)
                .ToListAsync();

            var byDate = attendances.ToDictionary(a => DateOnly.FromDateTime(a.AttendanceDate));
            var weekRestDays = GetWeeklyRestDayIndexes(user.WeekHoliday);
            var today = DateOnly.FromDateTime(DateTime.Today);

            var result = new EmployeeMonthlyReportResult
            {
                UserId = user.Id,
                EmployeeCode = user.Code,
                EmployeeName = user.FullName,
                BranchName = user.Branch?.Name ?? "",
                Month = month,
                Year = year
            };

            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                bool isWeeklyRest = weekRestDays.Contains((int)d.DayOfWeek);

                if (byDate.TryGetValue(d, out var att))
                {
                    var day = new MonthlyAttendanceDay
                    {
                        AttendanceId = att.Id,
                        Date = d,
                        DayName = GetArabicDayName(d.DayOfWeek),
                        CheckInTime = att.CheckInTime?.TimeOfDay,
                        CheckOutTime = att.CheckOutTime?.TimeOfDay,
                        ShiftId = att.ShiftId,
                        ShiftName = att.Shift?.Name,
                        Late = att.Late,
                        EarlyLeave = att.EarlyLeave,
                        EarlyEnter = att.EarlyEnter,
                        Overtime = att.Overtime,
                        TotalWorkHours = att.TotalWorkHours,
                        IsAbsence = att.IsAbsence,
                        IsHoliday = att.IsHoliday,
                        IsWeeklyRest = isWeeklyRest,
                        HasLeave = att.LeaveId.HasValue,
                        ExemptLate = att.ExemptLate,
                        ExemptEarlyLeave = att.ExemptEarlyLeave,
                        ExemptEarlyEnter = att.ExemptEarlyEnter,
                        ExemptOvertime = att.ExemptOvertime
                    };
                    result.Days.Add(day);
                }
                else
                {
                    bool isFuture = d > today;
                    result.Days.Add(new MonthlyAttendanceDay
                    {
                        Date = d,
                        DayName = GetArabicDayName(d.DayOfWeek),
                        ShiftId = user.ShiftId,
                        ShiftName = user.Shift?.Name,
                        IsWeeklyRest = isWeeklyRest,
                        IsAbsence = !isWeeklyRest && !isFuture,
                        ExemptLate = user.ExemptLate,
                        ExemptEarlyLeave = user.ExemptEarlyLeave,
                        ExemptEarlyEnter = user.ExemptEarlyEnter,
                        ExemptOvertime = user.ExemptOvertime
                    });
                }
            }

            result.TotalAbsenceDays = result.Days.Count(x => x.IsAbsence);
            result.TotalWeeklyRestDays = result.Days.Count(x => x.IsWeeklyRest && !x.IsAbsence);
            result.TotalHolidayDays = result.Days.Count(x => x.IsHoliday);
            result.TotalLate = Sum(result.Days.Select(x => x.Late));
            result.TotalOvertime = Sum(result.Days.Select(x => x.Overtime));
            result.TotalEarlyLeave = Sum(result.Days.Select(x => x.EarlyLeave));
            result.TotalEarlyEnter = Sum(result.Days.Select(x => x.EarlyEnter));
            result.TotalWorkHours = Sum(result.Days.Select(x => x.TotalWorkHours));

            return result;
        }

        public async Task SaveDayAsync(int userId, DateOnly date, TimeSpan? checkIn, TimeSpan? checkOut,
            bool isAbsence, bool isHoliday, bool exemptLate, bool exemptEarlyLeave, bool exemptEarlyEnter, bool exemptOvertime)
        {
            var user = await _db.Users.Include(u => u.Shift).FirstOrDefaultAsync(u => u.Id == userId)
                                   ?? throw new Exception("الموظف غير موجود");
    
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var attendance = await _db.Attendances
                            .FirstOrDefaultAsync(a => a.UserId == userId && a.AttendanceDate == dayStart);
    
            if (attendance == null)
            {
                attendance = new Attendance
                {
                    UserId = userId,
                    AttendanceDate = dayStart,
                    ShiftId = user.ShiftId,
                    CheckInBranchId = user.BranchId,
                    CheckOutBranchId = user.BranchId
                };
                _db.Attendances.Add(attendance);
            }
    
            attendance.IsAbsence = isAbsence;
            attendance.IsHoliday = isHoliday;
            attendance.ExemptLate = exemptLate;
            attendance.ExemptEarlyLeave = exemptEarlyLeave;
            attendance.ExemptEarlyEnter = exemptEarlyEnter;
            attendance.ExemptOvertime = exemptOvertime;
    
            if (!isAbsence && !isHoliday && checkIn.HasValue && checkOut.HasValue)
            {
                attendance.CheckInTime = date.ToDateTime(TimeOnly.FromTimeSpan(checkIn.Value));
                attendance.CheckOutTime = date.ToDateTime(TimeOnly.FromTimeSpan(checkOut.Value));
        
                var shiftId = attendance.ShiftId ?? user.ShiftId;
                var shift = await _db.Shifts.FindAsync(shiftId);
                RecalculateTimes(attendance, shift, exemptLate, exemptEarlyLeave, exemptEarlyEnter, exemptOvertime);
            }
            else
            {
                attendance.CheckInTime = null;
                attendance.CheckOutTime = null;
                attendance.Late = null;
                attendance.EarlyLeave = null;
                attendance.EarlyEnter = null;
                attendance.Overtime = null;
                attendance.TotalWorkHours = null;
            }
    
            await _db.SaveChangesAsync();
        }

        // ===================== تغيير الوردية =====================

        public async Task ChangeShiftAsync(int userId, DateOnly date, int newShiftId)
        {
            var shift = await _db.Shifts.FindAsync(newShiftId) ?? throw new Exception("الوردية غير موجودة");
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId) ?? throw new Exception("الموظف غير موجود");
    
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var attendance = await _db.Attendances
                            .FirstOrDefaultAsync(a => a.UserId == userId && a.AttendanceDate == dayStart);
    
            if (attendance == null)
            {
                attendance = new Attendance
                {
                    UserId = userId,
                    AttendanceDate = dayStart,
                    ShiftId = newShiftId,
                    CheckInBranchId = user.BranchId,
                    CheckOutBranchId = user.BranchId
                };
                _db.Attendances.Add(attendance);
            }
            else
            {
                attendance.ShiftId = newShiftId;
                if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue && !attendance.IsAbsence && !attendance.IsHoliday)
                {
                    RecalculateTimes(attendance, shift, attendance.ExemptLate, attendance.ExemptEarlyLeave, attendance.ExemptEarlyEnter, attendance.ExemptOvertime);
                }
            }
    
            await _db.SaveChangesAsync();
        }

        public async Task<(int? ShiftId, string? ShiftName)> GetShiftForDateAsync(int userId, DateOnly date)
        {
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var attendance = await _db.Attendances
                    .Include(a => a.Shift)
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.AttendanceDate == dayStart);
    
            if (attendance != null)
                return (attendance.ShiftId, attendance.Shift?.Name);
    
            var user = await _db.Users.Include(u => u.Shift).FirstOrDefaultAsync(u => u.Id == userId);
            return (user?.ShiftId, user?.Shift?.Name);
        }

        void RecalculateTimes(Attendance attendance, Shift ? shift, bool exemptLate, bool exemptEarlyLeave, bool exemptEarlyEnter, bool exemptOvertime)
        {
            if (shift == null || attendance.CheckInTime == null || attendance.CheckOutTime == null) return;
    
            var clockIn = attendance.CheckInTime.Value.TimeOfDay;
            var clockOut = attendance.CheckOutTime.Value.TimeOfDay;
            if (clockOut < clockIn) clockOut = TimeSpan.FromDays(1);
    
            var onDuty = shift.StartTime;
            var offDuty = shift.EndTime;
    
            attendance.Late = (!exemptLate && clockIn > onDuty) ? clockIn - onDuty : TimeSpan.Zero;
            attendance.EarlyLeave = (!exemptEarlyLeave && clockOut < offDuty) ? offDuty - clockOut : TimeSpan.Zero;
            attendance.EarlyEnter = (!exemptEarlyEnter && clockIn < onDuty) ? onDuty - clockIn : TimeSpan.Zero;
            attendance.Overtime = (!exemptOvertime && clockOut > offDuty) ? clockOut - offDuty : TimeSpan.Zero;
            attendance.TotalWorkHours = clockOut - clockIn;
        }

        // ===================== التقرير الشهري (كل الموظفين) =====================

        public async Task<List<EmployeeMonthlySummaryItem>> GetMonthlySummaryAsync(int? branchId, string? search, int month, int year)
        {
            var(startDate, endDate) = GetMonthRange(month, year);
            var startDt = startDate.ToDateTime(TimeOnly.MinValue);
            var endDt = endDate.ToDateTime(TimeOnly.MaxValue);
    
            var usersQuery = _db.Users.Include(u => u.Branch).Where(u => !u.IsArchived).AsQueryable();
            if (branchId.HasValue) usersQuery = usersQuery.Where(u => u.BranchId == branchId.Value);
            if (!string.IsNullOrWhiteSpace(search)) usersQuery = usersQuery.Where(u => u.FullName.Contains(search) || u.Code.Contains(search));
    
            var users = await usersQuery.OrderBy(u => u.FullName).ToListAsync();
            var userIds = users.Select(u => u.Id).ToList();
    
            var attendances = await _db.Attendances
                    .Where(a => userIds.Contains(a.UserId) && a.AttendanceDate >= startDt && a.AttendanceDate <= endDt)
                    .ToListAsync();
    
            var grouped = attendances.GroupBy(a => a.UserId).ToDictionary(g => g.Key, g => g.ToList());
    
            var items = new List<EmployeeMonthlySummaryItem>();
            foreach (var u in users)
            {
                grouped.TryGetValue(u.Id, out var list);
                list ??= new List<Attendance>();
        
                items.Add(new EmployeeMonthlySummaryItem
                {
                    UserId = u.Id,
                    EmployeeCode = u.Code,
                    EmployeeName = u.FullName,
                    BranchName = u.Branch?.Name ?? "",
                    TotalAbsenceDays = list.Count(a => a.IsAbsence),
                    TotalHolidayDays = list.Count(a => a.IsHoliday),
                    TotalLate = Sum(list.Select(a => a.Late)),
                    TotalOvertime = Sum(list.Select(a => a.Overtime)),
                    TotalWorkHours = Sum(list.Select(a => a.TotalWorkHours))
                });
            }
    
            return items;
        }

        // ===================== أدوات مساعدة =====================

        static (DateOnly Start, DateOnly End) GetMonthRange(int month, int year)
        {
            var start = new DateOnly(year, month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }

        static List<int> GetWeeklyRestDayIndexes(WeekHoliday? wh)
        {
            var list = new List<int>();
                if (wh == null) return list;
                // Day1..Day7 يقابلوا السبت..الجمعة (نفس ترتيب DaysSummary في موديل WeekHoliday)
                if (wh.Day1) list.Add((int)DayOfWeek.Saturday);
                if (wh.Day2) list.Add((int)DayOfWeek.Sunday);
                if (wh.Day3) list.Add((int)DayOfWeek.Monday);
                if (wh.Day4) list.Add((int)DayOfWeek.Tuesday);
                if (wh.Day5) list.Add((int)DayOfWeek.Wednesday);
                if (wh.Day6) list.Add((int)DayOfWeek.Thursday);
                if (wh.Day7) list.Add((int)DayOfWeek.Friday);
                return list;
            }

        static string GetArabicDayName(DayOfWeek day) => day switch
        {
            DayOfWeek.Saturday => "السبت",
            DayOfWeek.Sunday => "الأحد",
            DayOfWeek.Monday => "الإثنين",
            DayOfWeek.Tuesday => "الثلاثاء",
            DayOfWeek.Wednesday => "الأربعاء",
            DayOfWeek.Thursday => "الخميس",
            DayOfWeek.Friday => "الجمعة",
            _ => ""
        };

        static TimeSpan Sum(IEnumerable<TimeSpan?> values)
        {
            var total = TimeSpan.Zero;
                foreach (var v in values) total += v ?? TimeSpan.Zero;
                return total;
        }
    }
}