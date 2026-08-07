using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class SettingService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<SettingService> _logger;

        public SettingService(IDbContextFactory<AppDbContext> dbFactory, ILogger<SettingService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<Setting> GetSettingsAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.Settings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new Setting
                {
                    CompanyName = "شركة",
                    StartOfMonth = 26,
                    EndOfMonth = 25,
                    LateOvertimeCalculationMode = 0,
                    LateRepeat = 3,
                    LateValue = 1
                };
                db.Settings.Add(settings);
                await db.SaveChangesAsync();
            }

            return settings;
        }

        public async Task<(bool, string)> UpdateSettingsAsync(Setting settings)
        {
            try
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                var existing = await db.Settings.FindAsync(settings.Id);

                if (existing == null)
                {
                    settings.Id = 0;
                    db.Settings.Add(settings);
                }
                else
                {
                    existing.CompanyName = settings.CompanyName;
                    existing.CentralDocumentStoragePath = settings.CentralDocumentStoragePath;
                    existing.StartOfMonth = settings.StartOfMonth;
                    existing.EndOfMonth = settings.EndOfMonth;
                    existing.LateOvertimeCalculationMode = settings.LateOvertimeCalculationMode;
                    existing.LateRepeat = settings.LateRepeat;
                    existing.LateValue = settings.LateValue;
                    existing.UpdatedAt = DateTime.Now;
                }

                await db.SaveChangesAsync();
                return (true, "تم حفظ الإعدادات بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء حفظ الإعدادات");
                return (false, $"حدث خطأ: {ex.Message}");
            }
        }
    }
}