using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class TaskService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        public TaskService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

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
            await _db.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int taskId, UserTaskStatus status)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var task = await _db.UserTasks.FindAsync(taskId) ?? throw new Exception("المهمة غير موجودة");
            task.Status = (int)status;
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