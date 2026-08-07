using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class LateOvertimeService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public LateOvertimeService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// جلب كل القواعد
        /// </summary>
        public async Task<List<LateOvertime>> GetAllRulesAsync(int? type = null, int? moneyType = null)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.LateOvertimes.AsQueryable();

            if (type.HasValue)
                query = query.Where(r => r.Type == type.Value); // 0 = تأخير, 1 = إضافي

            if (moneyType.HasValue)
                query = query.Where(r => r.MoneyType == moneyType.Value); // 0 = دقائق, 1 = مالية

            return await query.OrderBy(r => r.StartTime).ToListAsync();
        }

        /// <summary>
        /// حساب قيمة التأخير بناءً على القواعد
        /// </summary>
        public async Task<decimal> CalculateLateValueAsync(TimeSpan lateTime, decimal minuteRate, int moneyType)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            // جلب قواعد التأخير
            var rules = await db.LateOvertimes
                .Where(r => r.Type == 0 && r.MoneyType == moneyType) // 0 = تأخير
                .OrderBy(r => r.StartTime)
                .ToListAsync();

            // البحث عن القاعدة المطابقة للوقت
            var matchingRule = rules.FirstOrDefault(r =>
                lateTime >= r.StartTime && lateTime <= r.EndTime);

            if (matchingRule == null) return 0;

            decimal lateMinutes = (decimal)lateTime.TotalMinutes;

            if (moneyType == 0) // دقائق (نسبة من سعر الدقيقة)
            {
                return lateMinutes * minuteRate * matchingRule.Value;
            }
            else // مالية (مبلغ ثابت)
            {
                return matchingRule.Value;
            }
        }

        /// <summary>
        /// حساب قيمة الإضافي بناءً على القواعد
        /// </summary>
        public async Task<decimal> CalculateOvertimeValueAsync(TimeSpan overtime, decimal minuteRate, int moneyType)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            // جلب قواعد الإضافي
            var rules = await db.LateOvertimes
                .Where(r => r.Type == 1 && r.MoneyType == moneyType) // 1 = إضافي
                .OrderBy(r => r.StartTime)
                .ToListAsync();

            // البحث عن القاعدة المطابقة للوقت
            var matchingRule = rules.FirstOrDefault(r =>
                overtime >= r.StartTime && overtime <= r.EndTime);

            if (matchingRule == null) return 0;

            decimal overtimeMinutes = (decimal)overtime.TotalMinutes;

            if (moneyType == 0) // دقائق (نسبة من سعر الدقيقة)
            {
                return overtimeMinutes * minuteRate * matchingRule.Value;
            }
            else // مالية (مبلغ ثابت)
            {
                return matchingRule.Value;
            }
        }

        /// <summary>
        /// إضافة قاعدة جديدة
        /// </summary>
        public async Task AddRuleAsync(LateOvertime rule)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            rule.CreatedAt = DateTime.Now;
            rule.EditedAt = DateTime.Now;
            db.LateOvertimes.Add(rule);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// تعديل قاعدة
        /// </summary>
        public async Task UpdateRuleAsync(LateOvertime rule)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            rule.EditedAt = DateTime.Now;
            db.LateOvertimes.Update(rule);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// حذف قاعدة
        /// </summary>
        public async Task DeleteRuleAsync(int id)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var rule = await db.LateOvertimes.FindAsync(id);
            if (rule != null)
            {
                db.LateOvertimes.Remove(rule);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// جلب إعدادات التأخير من Settings
        /// </summary>
        public async Task<(int LateType, decimal LateValue, int LateRepeat)> GetLateSettingsAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.Settings.FirstOrDefaultAsync();

            return (
                settings?.LateOvertimeCalculationMode ?? 0,  // LateType
                settings?.LateValue ?? 0,                     // LateValue
                settings?.LateRepeat ?? 0                     // LateRepeat
            );
        }

        /// <summary>
        /// حساب سعر الدقيقة للموظف
        /// </summary>
        public decimal CalculateMinuteRate(decimal monthlySalary, decimal monthlyWorkingHours = 208)
        {
            decimal monthlyMinutes = monthlyWorkingHours * 60;
            return monthlyMinutes > 0 ? monthlySalary / monthlyMinutes : 0;
        }
    }
}