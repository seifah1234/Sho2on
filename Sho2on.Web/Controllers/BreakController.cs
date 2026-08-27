using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BreakController : ControllerBase
    {
        private readonly AppDbContext _db;
        public BreakController(AppDbContext db) => _db = db;

        [HttpGet("my-break-type/{userId}")]
        public async Task<IActionResult> GetMyBreakType(int userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("الموظف غير موجود");
            if (user.BreakId == null) return Ok(new { HasBreak = false });

            var breakDef = await _db.Breaks.FindAsync(user.BreakId.Value);
            if (breakDef == null) return Ok(new { HasBreak = false });

            return Ok(new
            {
                HasBreak = true,
                BreakId = breakDef.Id,
                Name = breakDef.Name,
                Type = breakDef.Type.ToString(),
                DurationMinutes = breakDef.DurationMinutes,
                StartTime = breakDef.StartTime,
                EndTime = breakDef.EndTime
            });
        }

        [HttpGet("active/{userId}")]
        public async Task<IActionResult> GetActiveBreak(int userId)
        {
            var active = await _db.BreakLogs
                .Where(b => b.UserId == userId && b.EndTime == null)
                .OrderByDescending(b => b.StartTime)
                .FirstOrDefaultAsync();

            if (active == null) return Ok(new { IsActive = false });

            return Ok(new
            {
                IsActive = true,
                LogId = active.Id,
                StartTime = active.StartTime
            });
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartBreak([FromBody] StartBreakRequest req)
        {
            var user = await _db.Users.FindAsync(req.UserId);
            if (user == null) return NotFound("الموظف غير موجود");
            if (user.BreakId == null) return BadRequest("لا يوجد نظام استراحة مربوط بهذا الموظف");

            var alreadyOpen = await _db.BreakLogs.AnyAsync(b => b.UserId == req.UserId && b.EndTime == null);
            if (alreadyOpen) return BadRequest("يوجد استراحة مفتوحة بالفعل");

            var log = new BreakLog
            {
                UserId = req.UserId,
                BreakId = user.BreakId.Value,
                StartTime = DateTime.Now
            };
            _db.BreakLogs.Add(log);
            await _db.SaveChangesAsync();

            return Ok(new { Message = "تم بدء الاستراحة", LogId = log.Id, StartTime = log.StartTime });
        }

        [HttpPost("end/{userId}")]
        public async Task<IActionResult> EndBreak(int userId)
        {
            var log = await _db.BreakLogs
                .Include(b => b.Break)
                .Where(b => b.UserId == userId && b.EndTime == null)
                .OrderByDescending(b => b.StartTime)
                .FirstOrDefaultAsync();

            if (log == null) return BadRequest("لا توجد استراحة مفتوحة حاليًا");

            log.EndTime = DateTime.Now;

            var duration = log.EndTime.Value - log.StartTime;
            var allowedMinutes = log.Break?.DurationMinutes ?? 0;
            log.ExceededLimit = allowedMinutes > 0 && duration.TotalMinutes > allowedMinutes;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                Message = "تم إنهاء الاستراحة",
                DurationMinutes = (int)duration.TotalMinutes,
                ExceededLimit = log.ExceededLimit
            });
        }

        [HttpGet("today/{userId}")]
        public async Task<IActionResult> GetTodayBreaks(int userId)
        {
            var today = DateTime.Today;
            var logs = await _db.BreakLogs
                .Where(b => b.UserId == userId && b.StartTime.Date == today)
                .OrderBy(b => b.StartTime)
                .Select(b => new
                {
                    b.Id,
                    b.StartTime,
                    b.EndTime,
                    b.ExceededLimit
                })
                .ToListAsync();

            return Ok(logs);
        }
    }

    public class StartBreakRequest
    {
        public int UserId { get; set; }
    }
}