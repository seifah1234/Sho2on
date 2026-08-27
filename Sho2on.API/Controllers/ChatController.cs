using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ChatController(AppDbContext db) => _db = db;

        [HttpGet("conversations/{userId}")]
        public async Task<IActionResult> GetConversations(int userId)
        {
            var chats = await _db.Chats
                .Include(c => c.FirstUser).Include(c => c.SecondUser)
                .Where(c => c.FirstUserId == userId || c.SecondUserId == userId)
                .Select(c => new
                {
                    profileImageData = c.FirstUser.ProfileImageData,
                    OtherUserId = c.FirstUserId == userId ? c.SecondUserId : c.FirstUserId,
                    OtherUserName = c.FirstUserId == userId ? c.SecondUser.FullName : c.FirstUser.FullName,
                    LastMessage = c.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Message).FirstOrDefault(),
                    LastMessageTime = c.UpdatedAt ?? c.CreatedAt,
                    UnreadCount = c.Messages.Count(m => m.ReceiverId == userId && !m.IsRead)
                })
                .ToListAsync();

            return Ok(chats);
        }

        [HttpGet("messages/{currentUserId}/{otherUserId}")]
        public async Task<IActionResult> GetMessages(int currentUserId, int otherUserId)
        {
            var messages = await _db.ChatMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .Select(m => new { m.Id, m.SenderId, m.Message, m.SentAt })
                .ToListAsync();

            var unread = await _db.ChatMessages
                .Where(m => m.SenderId == otherUserId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();
            foreach (var m in unread) m.IsRead = true;
            if (unread.Count > 0) await _db.SaveChangesAsync();

            return Ok(messages);
        }

        // إضافة Endpoint للبحث عن المستخدمين
        [HttpGet("SearchUsers")]
        public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm)
        {
            var users = await _db.Users
                .Where(u => u.FullName.Contains(searchTerm) || u.Code.Contains(searchTerm))
                .Select(u => new { u.Id, u.FullName, u.Code, u.Department!.Name })
                .Take(20)
                .ToListAsync();

            return Ok(users);
        }

        // إضافة Endpoint للحصول على جميع المستخدمين
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _db.Users
                .Select(u => new { u.Id, u.FullName, u.Code, u.Department!.Name })
                .ToListAsync();

            return Ok(users);
        }
    }
}