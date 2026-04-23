using HR_Application.UserControls;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Threading.Tasks;
using System.Windows;
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

        // ✅ أحداث المجموعات
        public event Action<int, int, string, DateTime> OnGroupMessageReceived;

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
            App.SignalRConnection.Remove("ReceiveGroupMessage"); // ✅ إضافة

            // رسائل خاصة
            App.SignalRConnection.On<int, int, string, DateTime>(
                "ReceiveMessage",
                async (fromUserId, toUserId, message, timestamp) =>
                {
                    await UpdateUnreadCountAsync(fromUserId, toUserId);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        OnMessageReceived?.Invoke(fromUserId, toUserId, message, timestamp));
                });

            // ✅ رسائل المجموعات
            App.SignalRConnection.On<int, int, string, DateTime>(
                "ReceiveGroupMessage",
                async (groupId, senderId, message, timestamp) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        OnGroupMessageReceived?.Invoke(groupId, senderId, message, timestamp));
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

            _listenersRegistered = true;
        }

        // ✅ دالة للانضمام إلى مجموعة
        public async Task JoinGroupAsync(int groupId)
        {
            try
            {
                if (App.SignalRConnection?.State == HubConnectionState.Connected)
                {
                    await App.SignalRConnection.InvokeAsync("JoinGroup", groupId);
                    Console.WriteLine($"Joined group {groupId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JoinGroup error: {ex.Message}");
            }
        }

        // ✅ دالة لمغادرة مجموعة
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

        // ✅ إرسال رسالة مجموعة
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