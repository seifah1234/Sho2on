using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class SalarySettingService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<SalarySettingService> _logger;

        public SalarySettingService(
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<SalarySettingService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// جلب الإعدادات الحالية
        /// </summary>
        public async Task<SalarySetting> GetSettingsAsync()
        {

        using var _db = await _dbFactory.CreateDbContextAsync();
            var settings = await _db.SalarySettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                // إنشاء إعدادات افتراضية إذا لم توجد
                settings = new SalarySetting();
                _db.SalarySettings.Add(settings);
                await _db.SaveChangesAsync();
            }

            return settings;
        }

        /// <summary>
        /// تحديث الإعدادات
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateSettingsAsync(SalarySetting settings)
        {

            try
            {
        using var _db = await _dbFactory.CreateDbContextAsync();
                var existing = await _db.SalarySettings.FindAsync(settings.Id);
                if (existing == null)
                    return (false, "الإعدادات غير موجودة");

                existing.TaxPercentage = settings.TaxPercentage;
                existing.TaxThreshold = settings.TaxThreshold;
                existing.InsurancePercentage = settings.InsurancePercentage;
                existing.InsuranceMaxAmount = settings.InsuranceMaxAmount;
                existing.FriendshipBoxPercentage = settings.FriendshipBoxPercentage;
                existing.SocialParticipationAmount = settings.SocialParticipationAmount;
                existing.AllowOffCycle = settings.AllowOffCycle;
                existing.AbsenceDeductionRate = settings.AbsenceDeductionRate;
                existing.Currency = settings.Currency;
                existing.Notes = settings.Notes;
                existing.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();
                return (true, "تم تحديث الإعدادات بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تحديث إعدادات الرواتب");
                return (false, $"حدث خطأ: {ex.Message}");
            }
        }

        /// <summary>
        /// إعادة تعيين الإعدادات إلى القيم الافتراضية
        /// </summary>
        public async Task<(bool Success, string Message)> ResetToDefaultAsync()
        {
            try
            {
        using var _db = await _dbFactory.CreateDbContextAsync();
                var existing = await _db.SalarySettings.FirstOrDefaultAsync();
                if (existing == null)
                {
                    existing = new SalarySetting();
                    _db.SalarySettings.Add(existing);
                }

                // إعادة تعيين للقيم الافتراضية
                existing.TaxPercentage = 10;
                existing.TaxThreshold = 5000;
                existing.InsurancePercentage = 11;
                existing.InsuranceMaxAmount = 50000;
                existing.FriendshipBoxPercentage = 5;
                existing.SocialParticipationAmount = 0;
                existing.AllowOffCycle = true;
                existing.AbsenceDeductionRate = 1;
                existing.Currency = "ج.م";
                existing.Notes = "إعدادات افتراضية";
                existing.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();
                return (true, "تم إعادة تعيين الإعدادات للقيم الافتراضية");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء إعادة تعيين الإعدادات");
                return (false, $"حدث خطأ: {ex.Message}");
            }
        }
    }
}