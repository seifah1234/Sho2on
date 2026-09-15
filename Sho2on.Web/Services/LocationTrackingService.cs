using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class EmployeeLocationItem
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string? Code { get; set; }
        public string? JobTitleName { get; set; }
        public string? BranchName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public bool IsOnShift { get; set; }
        public bool IsRecentlyActive { get; set; }
    }

    public class LocationTrackingService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        // موظف يعتبر "أونلاين" لو آخر تحديث وصل خلال آخر دقيقتين
        private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(2);

        public LocationTrackingService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        // آخر موقع معروف لكل الموظفين اللي ليهم قراءة موقع، مع فلترة اختيارية بالفرع/البحث
        public async Task<List<EmployeeLocationItem>> GetAllLocationsAsync(
            int? branchId = null, string? search = null)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;

            var query = _db.EmployeeLiveLocations
                .Include(l => l.User)
                    .ThenInclude(u => u!.JobTitle)
                .Include(l => l.User)
                    .ThenInclude(u => u!.Branch)
                .Where(l => l.User != null)
                .AsQueryable();

            if (branchId.HasValue)
                query = query.Where(l => l.User!.BranchId == branchId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l =>
                    l.User!.FullName.Contains(search) ||
                    (l.User!.Code != null && l.User!.Code.Contains(search)));

            var rows = await query.ToListAsync();

            return rows
                .Select(l => new EmployeeLocationItem
                {
                    UserId = l.UserId,
                    FullName = l.User!.FullName,
                    Code = l.User.Code,
                    JobTitleName = l.User.JobTitle?.Name,
                    BranchName = l.User.Branch?.Name,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    Accuracy = l.Accuracy,
                    UpdatedAtUtc = l.UpdatedAtUtc,
                    IsOnShift = l.IsOnShift,
                    IsRecentlyActive = (now - l.UpdatedAtUtc) <= OnlineThreshold,
                })
                .OrderByDescending(x => x.IsRecentlyActive)
                .ThenByDescending(x => x.UpdatedAtUtc)
                .ToList();
        }
    }
}
