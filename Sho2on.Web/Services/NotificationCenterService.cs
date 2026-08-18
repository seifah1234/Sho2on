using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class NotificationCenterService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly InternalNotifyClient _pushClient;
        public NotificationCenterService(IDbContextFactory<AppDbContext> dbFactory, InternalNotifyClient pushClient)
        {
            _dbFactory = dbFactory;
            _pushClient = pushClient;
        }

        public async Task CreateAsync(int userId, string title, string message, string icon, string? url)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            db.Notifications.Add(new Notification { UserId = userId, Title = title, Message = message, Icon = icon, Url = url });
            await db.SaveChangesAsync();

            await _pushClient.PushAsync(userId, title, message, icon, url);
        }

        // إشعار لكل الأشخاص اللي ليهم صلاحية موافقة (بدل ما نحدد شخص واحد بس)
        public async Task CreateForApproversAsync(List<int> approverUserIds, string title, string message, string icon, string? url)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            foreach (var uid in approverUserIds)
                db.Notifications.Add(new Notification { UserId = uid, Title = title, Message = message, Icon = icon, Url = url });
            await db.SaveChangesAsync();

            foreach (var uid in approverUserIds)
                await _pushClient.PushAsync(uid, title, message, icon, url); 
        }

        public async Task<List<Notification>> GetRecentAsync(int userId, int count = 10)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            try
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                return await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

            }
            catch
            {
                return 0;
            }
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var n = await db.Notifications.FindAsync(notificationId);
            if (n != null) { n.IsRead = true; await db.SaveChangesAsync(); }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var unread = await db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            foreach (var n in unread) n.IsRead = true;
            await db.SaveChangesAsync();
        }
    }
}