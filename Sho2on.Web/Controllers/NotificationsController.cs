using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public NotificationsController(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;
        [HttpGet("unread-count/{userId}")]
        public async Task<IActionResult> GetUnreadCount(int userId)
        {
            using var _db = await _dbFactory.CreateDbContextAsync(); 
            var count = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            return Ok(new { UnreadCount = count });
        }
    }
}
