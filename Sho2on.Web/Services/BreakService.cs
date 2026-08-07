using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class BreakService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        public BreakService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

        public async Task<List<Break>> GetAllAsync()
        {
            var _db = await _dbFactory.CreateDbContextAsync();
            return await _db.Breaks.OrderBy(b => b.Name).ToListAsync();
        }

        public async Task SaveAsync(int? id, string name, BreakType type, TimeSpan? start, TimeSpan? end, int duration)
        {
            var _db = await _dbFactory.CreateDbContextAsync();
            Break b;
            if (id.HasValue) b = await _db.Breaks.FindAsync(id.Value) ?? throw new Exception("غير موجود");
            else { b = new Break(); _db.Breaks.Add(b); }

            b.Name = name;
            b.Type = type;
            b.StartTime = type == BreakType.Fixed ? start : null;
            b.EndTime = type == BreakType.Fixed ? end : null;
            b.DurationMinutes = type == BreakType.Flexible ? duration : null;
            b.EditedAt = DateTime.Now;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var _db = await _dbFactory.CreateDbContextAsync();
            var b = await _db.Breaks.FindAsync(id) ?? throw new Exception("غير موجود");
            _db.Breaks.Remove(b);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// بداية البريك - الموظف بيعمل بصمة بداية الراحة
        /// </summary>
        public async Task<(bool Success, string Message)> StartBreakAsync(int userId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            // جلب نظام الراحة للموظف
            var user = await db.Users
                .Include(u => u.Break)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Break == null)
                return (false, "لا يوجد نظام راحة محدد للموظف");

            // التأكد من عدم وجود بريك مفتوح
            var openBreak = await db.BreakLogs
                .FirstOrDefaultAsync(b => b.UserId == userId && b.EndTime == null);

            if (openBreak != null)
                return (false, "لديك بريك مفتوح بالفعل");

            var breakLog = new BreakLog
            {
                UserId = userId,
                BreakId = user.Break.Id,
                StartTime = DateTime.Now,
            };

            db.BreakLogs.Add(breakLog);
            await db.SaveChangesAsync();

            return (true, $"تم تسجيل بداية الراحة - المسموح: {breakLog.Break.DurationMinutes} دقيقة");
        }

        /// <summary>
        /// نهاية البريك - الموظف بيعمل بصمة نهاية الراحة
        /// </summary>
        public async Task<(bool Success, string Message)> EndBreakAsync(int userId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var openBreak = await db.BreakLogs
                .Include(b => b.Break)
                .FirstOrDefaultAsync(b => b.UserId == userId && b.EndTime == null);

            if (openBreak == null)
                return (false, "لا يوجد بريك مفتوح");

            openBreak.EndTime = DateTime.Now;
            var actualMinutes = (int)(openBreak.EndTime.Value - openBreak.StartTime).TotalMinutes;
            openBreak.ExceededLimit = actualMinutes > openBreak.Break.DurationMinutes;

            if (openBreak.ExceededLimit)
            {
                int extraMinutes = actualMinutes - (openBreak.Break.DurationMinutes ?? 0);
            }

            await db.SaveChangesAsync();

            return (true, $"تم تسجيل نهاية الراحة - المدة الفعلية: {actualMinutes} دقيقة");
        }

        /// <summary>
        /// جلب سجل الراحة للموظف في تاريخ محدد
        /// </summary>
        public async Task<List<BreakLog>> GetEmployeeBreakLogsAsync(int userId, DateTime date)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            return await db.BreakLogs
                .Include(b => b.Break)
                .Where(b => b.UserId == userId && b.StartTime >= dayStart && b.StartTime < dayEnd)
                .OrderBy(b => b.StartTime)
                .ToListAsync();
        }

        /// <summary>
        /// جلب تقرير البريك لموظف في شهر
        /// </summary>
        public async Task<BreakReportDto> GetBreakReportAsync(int userId, int month, int year)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            var logs = await db.BreakLogs
                .Include(b => b.Break)
                .Where(b => b.UserId == userId && b.StartTime >= start && b.StartTime < end && b.EndTime != null)
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            return new BreakReportDto
            {
                UserId = userId,
                Month = month,
                Year = year,
                TotalBreaks = logs.Count,
                OnTime = logs.Count(b => !b.ExceededLimit),
                Exceeded = logs.Count(b => b.ExceededLimit),
                TotalExtraMinutes = logs.Where(b => b.ExceededLimit).Sum(b => (int)(b.EndTime.Value - b.StartTime).TotalMinutes - (b.Break.DurationMinutes ?? 0)),
                Logs = logs
            };
        }
    }
}