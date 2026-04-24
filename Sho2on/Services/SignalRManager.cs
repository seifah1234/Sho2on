using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Application = System.Windows.Application; 

namespace HR_Application.Services
{
    public class SignalRManager
    {
        private static SignalRManager _instance;
        private static readonly object _lock = new object();
        public static SignalRManager Instance
        {
            get { lock (_lock) { return _instance ??= new SignalRManager(); } }
        }

        private bool _listenersRegistered = false;

        // Events that any Window can subscribe to
        public event Action<int, int, string, DateTime> OnMessageReceived;
        public event Action<int, int> OnMessageDelivered;
        public event Action<int, int> OnMessageRead;
        public event Action<string, int, int, string, DateTime> OnTaskNotification;
        public event Action<int> OnUnreadCountChanged;
        public event Action<int, int, DateTime> OnGroupMessagesRead;
        public event Action<int> OnMessageDeleted;
        public event Action<int, string> OnMessageEdited;
        public event Action<int, int, string, DateTime, string> OnGroupMessageReceived;

        // FIX BUG #4: New events for group message edit/delete
        public event Action<int, int, string> OnGroupMessageEdited;
        public event Action<int, int> OnGroupMessageDeleted;

        public async Task InitializeAsync(int userId)
        {
            try
            {
                if (App.SignalRConnection != null &&
                    App.SignalRConnection.State == HubConnectionState.Connected)
                {
                    await App.SignalRConnection.InvokeAsync("SetUserIdentifier", userId.ToString());
                    return;
                }

                var url = $"http://192.168.100.140:7001/chatHub?userId={userId}";

                App.SignalRConnection = new HubConnectionBuilder()
                    .WithUrl(url)
                    .WithAutomaticReconnect()
                    .Build();

                RegisterListeners();

                App.SignalRConnection.Reconnected += async (connectionId) =>
                {
                    await App.SignalRConnection.InvokeAsync("SetUserIdentifier", userId.ToString());
                };

                await App.SignalRConnection.StartAsync();
                await App.SignalRConnection.InvokeAsync("SetUserIdentifier", userId.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR init error: {ex.Message}");
            }
        }

        private void RegisterListeners()
        {
            if (_listenersRegistered) return;

            App.SignalRConnection.Remove("ReceiveMessage");
            App.SignalRConnection.Remove("MessageDelivered");
            App.SignalRConnection.Remove("MessageRead");
            App.SignalRConnection.Remove("ReceiveTaskNotification");
            App.SignalRConnection.Remove("ReceiveGroupMessage");
            App.SignalRConnection.Remove("GroupMessagesRead");
            App.SignalRConnection.Remove("MessageDeleted");
            App.SignalRConnection.Remove("MessageEdited");
            App.SignalRConnection.Remove("GroupMessageEdited");
            App.SignalRConnection.Remove("GroupMessageDeleted");

            App.SignalRConnection.On<int, int, string, DateTime>(
                "ReceiveMessage",
                async (fromUserId, toUserId, message, timestamp) =>
                {
                    await UpdateUnreadCountAsync(fromUserId, toUserId);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        OnMessageReceived?.Invoke(fromUserId, toUserId, message, timestamp));
                });


            App.SignalRConnection.On<int, int>(
                "MessageDelivered",
                async (fromUserId, toUserId) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        OnMessageDelivered?.Invoke(fromUserId, toUserId));
                });

            App.SignalRConnection.On<int, int>(
                "MessageRead",
                async (fromUserId, toUserId) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        OnMessageRead?.Invoke(fromUserId, toUserId));
                });

            App.SignalRConnection.On<string, int, int, string, DateTime>(
                "ReceiveTaskNotification",
                async (notificationType, taskId, fromUserId, taskDescription, timestamp) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        OnTaskNotification?.Invoke(notificationType, taskId, fromUserId, taskDescription, timestamp));
                });

            App.SignalRConnection.On<int, int, DateTime>(
                "GroupMessagesRead",
                async (groupId, userId, readAt) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        OnGroupMessagesRead?.Invoke(groupId, userId, readAt));
                });

            App.SignalRConnection.On<int>("MessageDeleted", async (messageId) =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    OnMessageDeleted?.Invoke(messageId));
            });

            App.SignalRConnection.On<int, string>("MessageEdited", async (messageId, newText) =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    OnMessageEdited?.Invoke(messageId, newText));
            });

            // ✅ FIXED: Correct parameter order to match server: (messageId, groupId, newText)
            App.SignalRConnection.On<int, int, string>("GroupMessageEdited", async (messageId, groupId, newText) =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    OnGroupMessageEdited?.Invoke(messageId, groupId, newText));
            });

            // ✅ FIXED: Correct parameter order to match server: (messageId, groupId)
            App.SignalRConnection.On<int, int>("GroupMessageDeleted", async (messageId, groupId) =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    OnGroupMessageDeleted?.Invoke(messageId, groupId));
            });

            App.SignalRConnection.On<int, int, string, DateTime, string>(
                "ReceiveGroupMessage",
                async (groupId, senderId, message, timestamp, senderName) =>
                {
                    if (senderId != App.CurrentUser?.Id)
                        await UpdateGroupUnreadCountAsync(groupId, senderId);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        OnGroupMessageReceived?.Invoke(
                            groupId, senderId, message, timestamp, senderName));
                });

            _listenersRegistered = true;
        }

        // FIX BUG #2: Update group unread and notify MainWindow
        private async Task UpdateGroupUnreadCountAsync(int groupId, int senderId)
        {
            try
            {
                using var ctx = new AppDbContext(App.ConnectionString);

                var member = await ctx.ChatGroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == groupId
                                           && m.UserId == App.CurrentUser.Id);
                if (member == null) return;

                member.UnreadCount++;
                await ctx.SaveChangesAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                    OnUnreadCountChanged?.Invoke(App.CurrentUser.Id));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateGroupUnreadCount error: {ex.Message}");
            }
        }

        // FIX BUG #2: Get total unread including groups
        public async Task<int> GetTotalUnreadAsync(int userId)
        {
            try
            {
                using var context = new AppDbContext(App.ConnectionString);

                // Get individual chat unread counts
                var chatUnread = await context.ChatUserStatuses
                    .Where(s => s.UserId == userId)
                    .SumAsync(s => s.UnreadCount);

                // Get group chat unread counts
                var groupUnread = await context.ChatGroupMembers
                    .Where(m => m.UserId == userId)
                    .SumAsync(m => m.UnreadCount);

                return chatUnread + groupUnread;
            }
            catch { return 0; }
        }

        public async Task JoinGroupAsync(int groupId)
        {
            try
            {
                if (App.SignalRConnection?.State == HubConnectionState.Connected)
                {
                    await App.SignalRConnection.InvokeAsync("JoinGroup", groupId);
                    Console.WriteLine($"Joined group {groupId} on SignalR server");
                }
                else
                {
                    Console.WriteLine($"Cannot join group {groupId}: SignalR not connected (State: {App.SignalRConnection?.State})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JoinGroup error for group {groupId}: {ex.Message}");
            }
        }

        public async Task LeaveGroupAsync(int groupId)
        {
            try
            {
                if (App.SignalRConnection?.State == HubConnectionState.Connected)
                {
                    await App.SignalRConnection.InvokeAsync("LeaveGroup", groupId);
                    Console.WriteLine($"Left group {groupId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LeaveGroup error: {ex.Message}");
            }
        }

        public async Task SendGroupMessageAsync(int groupId, int senderId, string message)
        {
            try
            {
                if (App.SignalRConnection?.State == HubConnectionState.Connected)
                {
                    await App.SignalRConnection.InvokeAsync("SendGroupMessage", groupId, senderId, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendGroupMessage error: {ex.Message}");
            }
        }

        private async Task UpdateUnreadCountAsync(int fromUserId, int toUserId)
        {
            try
            {
                using var context = new AppDbContext(App.ConnectionString);

                var chat = await context.Chats.FirstOrDefaultAsync(c =>
                    (c.FirstUserId == fromUserId && c.SecondUserId == toUserId) ||
                    (c.FirstUserId == toUserId && c.SecondUserId == fromUserId));

                if (chat == null) return;

                var status = await context.ChatUserStatuses
                    .FirstOrDefaultAsync(s => s.ChatId == chat.Id && s.UserId == toUserId);

                if (status == null)
                {
                    status = new ChatUserStatus
                    {
                        ChatId = chat.Id,
                        UserId = toUserId,
                        UnreadCount = 1
                    };
                    context.ChatUserStatuses.Add(status);
                }
                else
                {
                    status.UnreadCount++;
                }

                await context.SaveChangesAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                    OnUnreadCountChanged?.Invoke(toUserId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateUnreadCount error: {ex.Message}");
            }
        }

        // Add this method to SignalRManager class
        public async Task ResetAllUnreadCountsAsync(int userId)
        {
            try
            {
                using var context = new AppDbContext(App.ConnectionString);

                // Reset all individual chat unread counts
                var chatStatuses = await context.ChatUserStatuses
                    .Where(s => s.UserId == userId && s.UnreadCount > 0)
                    .ToListAsync();

                foreach (var status in chatStatuses)
                {
                    status.UnreadCount = 0;
                    status.LastReadAt = DateTime.Now;
                }

                // Reset all group chat unread counts
                var groupMembers = await context.ChatGroupMembers
                    .Where(m => m.UserId == userId && m.UnreadCount > 0)
                    .ToListAsync();

                foreach (var member in groupMembers)
                {
                    member.UnreadCount = 0;
                }

                await context.SaveChangesAsync();

                // Notify about the change
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    OnUnreadCountChanged?.Invoke(userId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ResetAllUnreadCounts error: {ex.Message}");
            }
        }

        public async Task ResetUnreadCountAsync(int chatId, int userId)
        {
            try
            {
                using var context = new AppDbContext(App.ConnectionString);
                var status = await context.ChatUserStatuses
                    .FirstOrDefaultAsync(s => s.ChatId == chatId && s.UserId == userId);

                if (status != null && status.UnreadCount > 0)
                {
                    status.UnreadCount = 0;
                    status.LastReadAt = DateTime.Now;
                    await context.SaveChangesAsync();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        OnUnreadCountChanged?.Invoke(userId));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ResetUnreadCount error: {ex.Message}");
            }
        }

        public async Task<int> GetUnreadForChatAsync(int chatId, int userId)
        {
            try
            {
                using var context = new AppDbContext(App.ConnectionString);
                var status = await context.ChatUserStatuses
                    .FirstOrDefaultAsync(s => s.ChatId == chatId && s.UserId == userId);
                return status?.UnreadCount ?? 0;
            }
            catch { return 0; }
        }
    }
}