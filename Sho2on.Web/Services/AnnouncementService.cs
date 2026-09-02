using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class AnnouncementService
    {
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public AnnouncementService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

        public async Task<List<Announcement>> GetAllAnnouncementsAsync()
        {
        using var db = await _dbFactory.CreateDbContextAsync(); 
            return await db.Announcements
                .Include(a => a.CreatedBy)
                .Include(a => a.AnnouncementType)
                .OrderByDescending(a => a.CreatedAt)
                .OrderBy(a => a.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<Announcement>> GetAnnouncementsAsync()
        {
        using var db = await _dbFactory.CreateDbContextAsync(); 
            return await db.Announcements
                .Include(a => a.CreatedBy)
                .Include(a => a.AnnouncementType)
                .Where(a => !a.IsDeleted && (!a.ExpireDate.HasValue || a.ExpireDate.Value.Date >= DateTime.Now.Date))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task CreateAnnouncement(Announcement announcement)
        {
            using var db = await _dbFactory.CreateDbContextAsync(); 
            await db.Announcements.AddAsync(announcement);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAnnouncement(Announcement announcement)
        {
        using var db = await _dbFactory.CreateDbContextAsync(); 
            db.Announcements.Update(announcement);
            await db.SaveChangesAsync();
        }


        public async Task<List<AnnouncementType>> GetAnnouncementTypesAsync()
        {
        using var db = await _dbFactory.CreateDbContextAsync(); 
            return await db.AnnouncementTypes.ToListAsync();
        }

        public async Task CreateAnnouncmentType(AnnouncementType announcementType)
        {
        using var db = await _dbFactory.CreateDbContextAsync(); 
            await db.AnnouncementTypes.AddAsync(announcementType);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAnnouncmentType(AnnouncementType announcementType)
        {
        using var db = await _dbFactory.CreateDbContextAsync(); 
            db.AnnouncementTypes.Update(announcementType);
            await db.SaveChangesAsync();
        }

        public async Task DeleteAnnouncmentType(AnnouncementType announcementType)
        {
        using var db = await _dbFactory.CreateDbContextAsync(); 
            db.AnnouncementTypes.Remove(announcementType);
            await db.SaveChangesAsync();
        }

        public async Task<AnnouncementType?> GetAnnouncementTypeByIdAsync(int id)
        {
        using var db = await _dbFactory.CreateDbContextAsync(); 
            return await db.AnnouncementTypes.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Announcement?> GetAnnouncementByIdAsync(int id)
        {
        using var db = await _dbFactory.CreateDbContextAsync(); 
            return await db.Announcements.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task DeleteAnnouncement(Announcement announcement)
        {
        using var db = await _dbFactory.CreateDbContextAsync(); 
            db.Announcements.Remove(announcement);
            await db.SaveChangesAsync();
        }

        public async Task<List<Announcement>> GetAnnouncementsByTypeIdAsync(int typeId)
        {
        using var db = await _dbFactory.CreateDbContextAsync(); 
            return await db.Announcements
                .Where(a => a.AnnouncementTypeId == typeId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}
