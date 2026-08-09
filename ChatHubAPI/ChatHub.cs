using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Security.Claims;

namespace ChatHubAPI
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        public ChatHub(AppDbContext context) => _context = context;

        int CurrentUserId => int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        public async Task SendMessageToUser(int toUserId, string message)
        {
            var fromUserId = CurrentUserId;

            var chat = await _context.Chats.FirstOrDefaultAsync(c =>
                (c.FirstUserId == fromUserId && c.SecondUserId == toUserId) ||
                (c.FirstUserId == toUserId && c.SecondUserId == fromUserId));

            if (chat == null)
            {
                chat = new Chat { FirstUserId = fromUserId, SecondUserId = toUserId, CreatedAt = DateTime.Now };
                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();
            }

            var chatMessage = new ChatMessage
            {
                ChatId = chat.Id,
                SenderId = fromUserId,
                ReceiverId = toUserId,
                Message = message,
                SentAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(chatMessage);
            chat.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await Clients.User(toUserId.ToString())
                .SendAsync("ReceiveMessage", fromUserId, toUserId, message, chatMessage.SentAt, chatMessage.Id);

            // نبعت نسخة للمرسل نفسه كمان (لو فاتح الشات من أكتر من تبويب/جهاز)
            await Clients.User(fromUserId.ToString())
                .SendAsync("MessageSent", toUserId, message, chatMessage.SentAt, chatMessage.Id);
        }

        public async Task SendGroupMessage(int groupId, string message)
        {
            var fromUserId = CurrentUserId;

            var isMember = await _context.ChatGroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == fromUserId);
            if (!isMember) throw new HubException("لست عضوًا في هذا الجروب");

            var groupMessage = new ChatGroupMessage
            {
                GroupId = groupId,
                SenderId = fromUserId,
                Message = message,
                SentAt = DateTime.Now
            };
            _context.ChatGroupMessages.Add(groupMessage);

            var members = await _context.ChatGroupMembers.Where(m => m.GroupId == groupId).ToListAsync();
            foreach (var member in members.Where(m => m.UserId != fromUserId))
                member.UnreadCount++;

            await _context.SaveChangesAsync();

            await Clients.Group($"group-{groupId}")
                .SendAsync("ReceiveGroupMessage", groupId, fromUserId, message, groupMessage.SentAt, groupMessage.Id);
        }

        public async Task JoinGroup(int groupId)
        {
            var isMember = await _context.ChatGroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == CurrentUserId);
            if (isMember)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"group-{groupId}");
        }

        public override async Task OnConnectedAsync()
        {
            var myGroups = await _context.ChatGroupMembers
                .Where(m => m.UserId == CurrentUserId)
                .Select(m => m.GroupId)
                .ToListAsync();

            foreach (var groupId in myGroups)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"group-{groupId}");

            var userId = Context.UserIdentifier;
            Console.WriteLine($"User connected: {userId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"User disconnected: {Context.UserIdentifier}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}