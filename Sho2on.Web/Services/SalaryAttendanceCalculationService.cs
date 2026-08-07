using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class SalaryAttendanceCalculationService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        public SalaryAttendanceCalculationService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

        public async Task<MonthSettings> GetMonthSettingsAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.Settings.FirstOrDefaultAsync();
            return new MonthSettings
            {
                StartDay = settings?.StartOfMonth ?? 26,
                EndDay = settings?.EndOfMonth ?? 25
            };
        }

        public (DateTime Start, DateTime End) GetCustomMonthDates(int month, int year, MonthSettings settings)
        {
            int startDay = settings.StartDay;
            int endDay = settings.EndDay;

            DateTime startDate = new DateTime(year, month, startDay);
            DateTime endDate = new DateTime(year, month, endDay);

            if (endDay < startDay)
            {
                startDate = startDate.AddMonths(-1);
            }

            return (startDate, endDate);
        }

        public async Task<AttendanceSalarySummary> CalculateAsync(int userId, int month, int year)
        {
        using var _db = await _dbFactory.CreateDbContextAsync();
            var user = await _db.Users.FindAsync(userId) ?? throw new Exception("الموظف غير موجود");
            var settings = await _db.Settings.FirstOrDefaultAsync();
            bool isPercentageMode = (settings?.LateOvertimeCalculationMode ?? 0) == 0;

            var monthSettings = await GetMonthSettingsAsync();
            var (startDate, endDate) = GetCustomMonthDates(month, year, monthSettings);

            var attendances = await _db.Attendances
                .Where(a => a.UserId == userId && a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();

            var lateTiers = await _db.LateOvertimes.Where(l => l.Type == 0 && l.MoneyType == (isPercentageMode ? 0 : 1)).ToListAsync();
            var overtimeTiers = await _db.LateOvertimes.Where(o => o.Type == 1 && o.MoneyType == (isPercentageMode ? 0 : 1)).ToListAsync();
            var absenceTiers = await _db.AbsenceTiers.OrderBy(t => t.FromOccurrence).ToListAsync();

            // ✅ التعامل مع NULL values
            decimal minSalaryRate = user.MinSalary ?? 0;  // استخدام Null-coalescing
            decimal mainSalary = user.MainSalary ?? 0;    // استخدام Null-coalescing
            decimal dailyRate = mainSalary / 30m;

            var summary = new AttendanceSalarySummary
            {
                UserId = userId,
                EmployeeName = user.FullName ?? "غير معروف",
                MainSalary = mainSalary,
                MinSalary = minSalaryRate
            };

            int absenceOccurrenceCounter = 0;

            foreach (var att in attendances)
            {
                var day = new AttendanceSalaryDay
                {
                    Date = DateOnly.FromDateTime(att.AttendanceDate),
                    IsAbsence = att.IsAbsence,
                    IsHoliday = att.IsHoliday,
                    Late = att.Late,
                    Overtime = att.Overtime,
                    EarlyLeave = att.EarlyLeave
                };

                if (att.IsAbsence && !user.ExemptAbsence)
                {
                    absenceOccurrenceCounter++;
                    summary.AbsenceDays++;

                    var tier = absenceTiers.FirstOrDefault(t =>
                        absenceOccurrenceCounter >= t.FromOccurrence &&
                        (t.ToOccurrence == null || absenceOccurrenceCounter <= t.ToOccurrence));

                    decimal multiplier = tier?.DeductionMultiplier ?? 1;
                    decimal thisDayDeduction = dailyRate * multiplier;

                    day.LateDeduction = 0;
                    summary.AbsenceDeductionValue += thisDayDeduction;
                }
                else if (att.IsHoliday)
                {
                    summary.WeeklyRestDays++;
                }
                else
                {
                    if (att.Late.HasValue && !user.ExemptLate)
                    {
                        day.LateDeduction = CalculateTierValue(att.Late.Value, lateTiers, minSalaryRate, isPercentageMode);
                        summary.TotalLateValue += day.LateDeduction;
                    }

                    if (att.Overtime.HasValue && !user.ExemptOvertime)
                    {
                        day.OvertimeBonus = CalculateTierValue(att.Overtime.Value, overtimeTiers, minSalaryRate, isPercentageMode);
                        summary.TotalOvertimeValue += day.OvertimeBonus;
                    }
                }

                summary.Days.Add(day);
            }

            return summary;
        }

        private decimal CalculateTierValue(TimeSpan duration, List<Database.Models.LateOvertime> tiers, decimal minSalaryRate, bool isPercentageMode)
        {
            var tier = tiers.FirstOrDefault(t => duration >= t.StartTime && duration <= t.EndTime);
            if (tier == null) return 0;

            if (isPercentageMode)
            {
                decimal totalMinutes = duration.Hours * 60 + duration.Minutes;
                return totalMinutes * tier.Value * minSalaryRate;
            }

            return tier.Value;
        }
    }
}