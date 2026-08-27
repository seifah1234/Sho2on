using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.DTOs;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TasksController(AppDbContext db) => _db = db;

        [HttpGet("assigned-to/{userId}")]
        public async Task<IActionResult> GetAssignedToMe(int userId)
        {
            var tasks = await _db.UserTasks
                .Include(t => t.AssignedByUser)
                .Where(t => t.AssignedToUserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Description,
                    Type = (UserTaskType)t.Type,
                    Status = (UserTaskStatus)t.Status,
                    AssignedBy = t.AssignedByUser.FullName,
                    t.CreatedAt,
                    t.DueDate
                })
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpGet("assigned-by/{userId}")]
        public async Task<IActionResult> GetAssignedbyMe(int userId)
        {
            var tasks = await _db.UserTasks
                .Include(t => t.AssignedToUser)
                .Where(t => t.AssignedByUserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Description,
                    Type = (UserTaskType)t.Type,
                    Status = (UserTaskStatus)t.Status,
                    AssignedBy = t.AssignedByUser.FullName,
                    t.CreatedAt,
                    t.DueDate
                })
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTask(TaskDto taskDto)
        {
            var task = new UserTask
            {
                AssignedByUserId = taskDto.AssignedByUserId,
                AssignedToUserId = taskDto.AssignedToUserId,
                CreatedAt = DateTime.Now,
                Description = taskDto.Description,
                Status = (int)UserTaskStatus.Sent,
                Type = (taskDto.Type == "task") ? (int)UserTaskType.Task : (int)UserTaskType.Order
            };

            await _db.UserTasks.AddAsync(task);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                sucess = true,
                data = task
            });

        }

        [HttpPut("{taskId}/status")]
        public async Task<IActionResult> UpdateStatus(int taskId, [FromBody] int newStatus)
        {
            var task = await _db.UserTasks.FindAsync(taskId);
            if (task == null) return NotFound("المهمة غير موجودة");

            task.Status = newStatus;
            await _db.SaveChangesAsync();
            return Ok(new { Message = "تم تحديث الحالة" });
        }

        [HttpDelete("{taskId}")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var task = await _db.UserTasks.FindAsync(taskId);
            if (task == null) return NotFound("المهمة غير موجودة");

            _db.UserTasks.Remove(task);
            await _db.SaveChangesAsync();
            return Ok(new { Message = "تم حذف المهمة" });
        }
    }
}