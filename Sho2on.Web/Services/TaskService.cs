using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class TaskService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly NotificationCenterService _notify;
        public TaskService(IDbContextFactory<AppDbContext> dbFactory, NotificationCenterService notify)
        {
            _dbFactory = dbFactory;
            _notify = notify;
        }

        public async Task<List<TaskListItem>> GetAssignedToMeAsync(int userId)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            return await _db.UserTasks
                .Include(t => t.AssignedByUser)
                .Where(t => t.AssignedToUserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TaskListItem
                {
                    Id = t.Id,
                    Description = t.Description,
                    Type = (UserTaskType)t.Type,
                    Status = (UserTaskStatus)t.Status,
                    AssignedToName = "",
                    AssignedByName = t.AssignedByUser.FullName,
                    CreatedAt = t.CreatedAt,
                    DueDate = t.DueDate
                })
                .ToListAsync();
        }

        public async Task<List<TaskListItem>> GetAssignedByMeAsync(int userId)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            return await _db.UserTasks
                .Include(t => t.AssignedToUser)
                .Where(t => t.AssignedByUserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TaskListItem
                {
                    Id = t.Id,
                    Description = t.Description,
                    Type = (UserTaskType)t.Type,
                    Status = (UserTaskStatus)t.Status,
                    AssignedToName = t.AssignedToUser.FullName,
                    AssignedByName = "",
                    CreatedAt = t.CreatedAt,
                    DueDate = t.DueDate
                })
                .ToListAsync();
        }

        public async Task CreateAsync(string description, UserTaskType type, int assignedToUserId, int assignedByUserId, DateTime? dueDate)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            _db.UserTasks.Add(new UserTask
            {
                Description = description,
                Type = (int)type,
                Status = (int)UserTaskStatus.Sent,
                AssignedToUserId = assignedToUserId,
                AssignedByUserId = assignedByUserId,
                CreatedAt = DateTime.Now,
                DueDate = dueDate
            });

            var assignedByUser = await _db.Users.FindAsync(assignedByUserId);
            if (assignedByUser != null) {
                await _notify.CreateAsync(assignedToUserId,
                    "مهمة جديدة",
                    $"{assignedByUser.FullName} قام بتعيين مهمة جديدة عليك",
                    "bi-task",
                    "/tasks");
            }

            await _db.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int taskId, UserTaskStatus status)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var task = await _db.UserTasks.Include(u => u.AssignedByUser).FirstOrDefaultAsync(t => t.Id == taskId) ?? throw new Exception("المهمة غير موجودة");
            task.Status = (int)status;
            var assignedByUser = await _db.Users.FindAsync(task.AssignedByUserId);
            if (assignedByUser != null)
            {
                await _notify.CreateAsync(task.AssignedByUserId,
                    "تحديث حالة المهمة",
                    $"{task.AssignedToUser.FullName} قام بتحديث حالة المهمة إلى {status}",
                    "bi-task",
                    "/tasks");
            }
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int taskId)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var task = await _db.UserTasks.FindAsync(taskId) ?? throw new Exception("المهمة غير موجودة");
            _db.UserTasks.Remove(task);
            await _db.SaveChangesAsync();
        }
    }
}