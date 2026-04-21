using Microsoft.AspNetCore.SignalR;
using Sho2on.Database;

namespace ChatHubAPI
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendMessageToUser(int fromUserId, int toUserId, string message)
        {
            // إرسال للمستخدم المحدد
            await Clients.User(toUserId.ToString())
                .SendAsync("ReceiveMessage", fromUserId, toUserId, message, DateTime.Now);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
