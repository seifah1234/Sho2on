using Microsoft.AspNetCore.SignalR.Client;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
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
        public event Action<int> OnUnreadCountChanged; // broadcasts userId

        public async Task InitializeAsync(int userId)
        {
            try
            {
                if (App.SignalRConnection != null &&
                    App.SignalRConnection.State == HubConnectionState.Connected)
                {
                    // Already connected — re-register identity only
                    await App.SignalRConnection.InvokeAsync("SetUserIdentifier", userId.ToString());
                    return;
                }

                var url = $"http://192.168.100.140:7001/chatHub?userId={userId}";
                //var url = $"http://{App.ServerIP}:7001/chatHub?userId={userId}";

                App.SignalRConnection = new HubConnectionBuilder()
                    .WithUrl(url)
                    .WithAutomaticReconnect()
                    .Build();

                RegisterListeners();

                App.SignalRConnection.Reconnected += async (connectionId) =>
                {
                    // Re-register after reconnect
                    await App.SignalRConnection.InvokeAsync(
                        "SetUserIdentifier", userId.ToString());
                };

                await App.SignalRConnection.StartAsync();
                await App.SignalRConnection.InvokeAsync(
                    "SetUserIdentifier", userId.ToString());
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

            App.SignalRConnection.On<int, int, string, DateTime>(
                "ReceiveMessage",
                async (fromUserId, toUserId, message, timestamp) =>
                {
                    // 1. Update UnreadCount in DB
                    await UpdateUnreadCountAsync(fromUserId, toUserId);

                    // 2. Broadcast to all subscribers (MainWindow, ChatBox, etc.)
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
                        OnTaskNotification?.Invoke(
                            notificationType, taskId, fromUserId, taskDescription, timestamp));
                });

            _listenersRegistered = true;
        }

        // Updates ChatUserStatus.UnreadCount in DB
        private async Task UpdateUnreadCountAsync(int fromUserId, int toUserId)
        {
            try
            {
                using var context = new AppDbContext(App.ConnectionString);

                var chat = await context.Chats.FirstOrDefaultAsync(c =>
                    (c.FirstUserId == fromUserId && c.SecondUserId == toUserId) ||
                    (c.FirstUserId == toUserId && c.SecondUserId == fromUserId));

                if (chat == null) return;

                // toUserId is the RECEIVER — increment their unread count
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

                // Notify UI that unread count changed for toUserId
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    OnUnreadCountChanged?.Invoke(toUserId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateUnreadCount error: {ex.Message}");
            }
        }

        // Call this when user opens a chat — resets unread count
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

        // Returns total unread for current user across all chats
        public async Task<int> GetTotalUnreadAsync(int userId)
        {
            try
            {
                using var context = new AppDbContext(App.ConnectionString);
                return await context.ChatUserStatuses
                    .Where(s => s.UserId == userId)
                    .SumAsync(s => s.UnreadCount);
            }
            catch { return 0; }
        }

        // Returns unread for a specific chat
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