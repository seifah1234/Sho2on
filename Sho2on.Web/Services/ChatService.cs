using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class ChatService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        public ChatService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

        public async Task<List<ConversationListItem>> GetConversationsAsync(int userId)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();

            var directChats = await _db.Chats
                .Include(c => c.FirstUser).Include(c => c.SecondUser)
                .Where(c => (c.FirstUserId == userId || c.SecondUserId == userId) && c.IsActive)
                .Select(c => new ConversationListItem
                {
                    Id = c.Id,
                    IsGroup = false,
                    OtherUserId = c.FirstUserId == userId ? c.SecondUserId : c.FirstUserId,
                    DisplayName = c.FirstUserId == userId ? c.SecondUser.FullName : c.FirstUser.FullName,
                    LastMessage = c.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Message).FirstOrDefault() ?? "",
                    LastMessageTime = c.UpdatedAt ?? c.CreatedAt,
                    UnreadCount = c.Messages.Count(m => m.ReceiverId == userId && !m.IsRead)
                })
                .ToListAsync();

            var groupChats = await _db.ChatGroupMembers
                .Include(m => m.Group)
                .Where(m => m.UserId == userId && m.Group.IsActive)
                .Select(m => new ConversationListItem
                {
                    Id = m.GroupId,
                    IsGroup = true,
                    DisplayName = m.Group.Name,
                    LastMessage = m.Group.Messages.OrderByDescending(x => x.SentAt).Select(x => x.Message).FirstOrDefault() ?? "",
                    LastMessageTime = m.Group.Messages.OrderByDescending(x => x.SentAt).Select(x => x.SentAt).FirstOrDefault(),
                    UnreadCount = m.UnreadCount
                })
                .ToListAsync();

            return directChats.Concat(groupChats)
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();
        }

        public async Task<List<ChatMessageItem>> GetDirectMessagesAsync(int currentUserId, int otherUserId)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();

            var messages = await _db.ChatMessages
                .Include(m => m.Sender)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatMessageItem
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    Message = m.Message,
                    SentAt = m.SentAt
                })
                .ToListAsync();

            var unread = await _db.ChatMessages
                .Where(m => m.SenderId == otherUserId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();
            foreach (var m in unread) { m.IsRead = true; m.ReadAt = DateTime.Now; }
            if (unread.Count > 0) await _db.SaveChangesAsync();

            return messages;
        }

        public async Task<List<ChatMessageItem>> GetGroupMessagesAsync(int groupId, int currentUserId)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();

            var messages = await _db.ChatGroupMessages
                .Include(m => m.Sender)
                .Where(m => m.GroupId == groupId && !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatMessageItem
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    Message = m.Message,
                    SentAt = m.SentAt
                })
                .ToListAsync();

            var member = await _db.ChatGroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == currentUserId);
            if (member != null && member.UnreadCount > 0)
            {
                member.UnreadCount = 0;
                await _db.SaveChangesAsync();
            }

            return messages;
        }

        public async Task<int> CreateGroupAsync(string name, int createdByUserId, List<int> memberIds)
        {
            var group = new ChatGroup { Name = name, CreatedByUserId = createdByUserId, CreatedAt = DateTime.Now };
            using var _db = await _dbFactory.CreateDbContextAsync();
            _db.ChatGroups.Add(group);
            await _db.SaveChangesAsync();

            var allMembers = memberIds.Append(createdByUserId).Distinct();
            foreach (var uid in allMembers)
            {
                _db.ChatGroupMembers.Add(new ChatGroupMember
                {
                    GroupId = group.Id,
                    UserId = uid,
                    IsAdmin = uid == createdByUserId,
                    JoinedAt = DateTime.Now
                });
            }
            await _db.SaveChangesAsync();
            return group.Id;
        }
    }
}