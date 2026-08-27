using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class AnnouncementService
    {
        private readonly AppDbContext appDbContext;

        public AnnouncementService(AppDbContext appDbContext)
        {
            this.appDbContext = appDbContext;
        }

        public async Task<List<Announcement>> GetAllAnnouncementsAsync()
        {
            return await appDbContext.Announcements
                .Include(a => a.AnnouncementType)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Announcement>> GetAnnouncementsAsync()
        {
            return await appDbContext.Announcements
                .Include(a => a.AnnouncementType)
                .Where(a => !a.IsDeleted && (!a.ExpireDate.HasValue || a.ExpireDate.Value.Date >= DateTime.Now.Date))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task CreateAnnouncement(Announcement announcement)
        {
            await appDbContext.Announcements.AddAsync(announcement);
            await appDbContext.SaveChangesAsync();
        }

        public async Task UpdateAnnouncement(Announcement announcement)
        {
            appDbContext.Announcements.Update(announcement);
            await appDbContext.SaveChangesAsync();
        }


        public async Task<List<AnnouncementType>> GetAnnouncementTypesAsync()
        {
            return await appDbContext.AnnouncementTypes.ToListAsync();
        }

        public async Task CreateAnnouncmentType(AnnouncementType announcementType)
        {
            await appDbContext.AnnouncementTypes.AddAsync(announcementType);
            await appDbContext.SaveChangesAsync();
        }

        public async Task UpdateAnnouncmentType(AnnouncementType announcementType)
        {
            appDbContext.AnnouncementTypes.Update(announcementType);
            await appDbContext.SaveChangesAsync();
        }

        public async Task DeleteAnnouncmentType(AnnouncementType announcementType)
        {
            appDbContext.AnnouncementTypes.Remove(announcementType);
            await appDbContext.SaveChangesAsync();
        }

        public async Task<AnnouncementType?> GetAnnouncementTypeByIdAsync(int id)
        {
            return await appDbContext.AnnouncementTypes.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Announcement?> GetAnnouncementByIdAsync(int id)
        {
            return await appDbContext.Announcements.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task DeleteAnnouncement(Announcement announcement)
        {
            appDbContext.Announcements.Remove(announcement);
            await appDbContext.SaveChangesAsync();
        }

        public async Task<List<Announcement>> GetAnnouncementsByTypeIdAsync(int typeId)
        {
            return await appDbContext.Announcements
                .Where(a => a.AnnouncementTypeId == typeId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}
