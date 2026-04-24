using DocumentFormat.OpenXml.Vml;
using HR_Application.Services;
using HR_Application.UserControls;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace HR_Application.Views.Conversations
{
    public partial class ChatWindow : Window, INotifyPropertyChanged
    {
        private readonly AppDbContext _context;
        private ObservableCollection<ChatItemData> _chatList;
        private ObservableCollection<UserSearchResult> _searchResults;
        private List<User> allUsers = new List<User>();
        private User _currentUser;
        private Dictionary<int, int> _unreadMessagesCount = new Dictionary<int, int>();
        private int _selectedUserId;
        public int SelectedUserId
        {
            get => _selectedUserId;
            set { _selectedUserId = value; OnPropertyChanged(); }
        }
        public ObservableCollection<ChatItemData> ChatList
        {
            get => _chatList;
            set
            {
                _chatList = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<GroupItemData> GroupList { get; set; } = new();
        public ObservableCollection<ChatItemData> ArchivedChatList { get; set; } = new();

        public ObservableCollection<UserSearchResult> SearchResults
        {
            get => _searchResults;
            set
            {
                _searchResults = value;
                OnPropertyChanged();
            }
        }

        public ChatWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            ChatList = new ObservableCollection<ChatItemData>();
            SearchResults = new ObservableCollection<UserSearchResult>();
            DataContext = this;
            _currentUser = App.CurrentUser;
            ChatBoxControl.NewMessageReceived += ChatBoxControl_NewMessageReceived;
            ChatBoxControl.NewMessageSent += ChatBoxControl_NewMessageSent;

            // FIX BUG #5: Subscribe to message updates (edit/delete)
            ChatBoxControl.MessageUpdated += ChatBoxControl_MessageUpdated;
            GroupChatBoxControl.GroupMessageUpdated += GroupChatBoxControl_GroupMessageUpdated;

            Loaded += async (s, e) =>
            {
                await LoadChatsAsync();
                await LoadGroupsAsync();
                await LoadArchivedChatsAsync();
                SetupGroupSignalRListener();

                // FIX BUG #2: Reset unread counts when window opens
                await RefreshUnreadCounts();
            };
        }

        // FIX BUG #2: Reset unread counts when chat window opens
        private async Task RefreshUnreadCounts()
        {
            try
            {
                // Keep individual chat unread counts for display in ChatWindow
                // Just reset the MainWindow badge
                await SignalRManager.Instance.ResetAllUnreadCountsAsync(App.CurrentUser.Id);

                // But keep the visual indicators in ChatWindow
                // (don't reset _unreadMessagesCount dictionary)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RefreshUnreadCounts error: {ex.Message}");
            }
        }

        // FIX BUG #5: Handle message updates (edit/delete) to update last message
        private void ChatBoxControl_MessageUpdated(object sender, MessageUpdatedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var chat = ChatList.FirstOrDefault(c => c.UserId == e.OtherUserId);
                if (chat != null)
                {
                    chat.LastMessage = e.LastMessage;
                    chat.LastMessageTime = e.LastMessageTime;

                    // ✅ FIX: Force complete UI refresh
                    var index = ChatList.IndexOf(chat);
                    if (index >= 0)
                    {
                        ChatList.RemoveAt(index);
                        ChatList.Insert(index, chat);
                    }

                    // Also update archived chats if present
                    var archivedChat = ArchivedChatList.FirstOrDefault(c => c.UserId == e.OtherUserId);
                    if (archivedChat != null)
                    {
                        archivedChat.LastMessage = e.LastMessage;
                        archivedChat.LastMessageTime = e.LastMessageTime;
                    }
                }
            });
        }

        // FIX BUG #3: Handle group message updates
        private void GroupChatBoxControl_GroupMessageUpdated(object sender, GroupMessageUpdatedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var group = GroupList.FirstOrDefault(g => g.GroupId == e.GroupId);
                if (group != null)
                {
                    group.LastMessage = e.LastMessage;
                    group.LastMessageTime = e.LastMessageTime;

                    // Force UI refresh
                    var index = GroupList.IndexOf(group);
                    GroupList.RemoveAt(index);
                    GroupList.Insert(index, group);
                }
            });
        }

        private void ChatsTab_Click(object sender, RoutedEventArgs e)
        {
            ChatsScrollViewer.Visibility = Visibility.Visible;
            GroupsScrollViewer.Visibility = Visibility.Collapsed;
            ArchiveScrollViewer.Visibility = Visibility.Collapsed;
            ChatBoxControl.Visibility = Visibility.Visible;
            GroupChatBoxControl.Visibility = Visibility.Collapsed;
            GroupsTab.IsChecked = false;
            ArchiveTab.IsChecked = false;
        }

        private void GroupsTab_Click(object sender, RoutedEventArgs e)
        {
            ChatsScrollViewer.Visibility = Visibility.Collapsed;
            GroupsScrollViewer.Visibility = Visibility.Visible;
            ArchiveScrollViewer.Visibility = Visibility.Collapsed;
            ChatBoxControl.Visibility = Visibility.Collapsed;
            GroupChatBoxControl.Visibility = Visibility.Visible;
            ChatsTab.IsChecked = false;
            ArchiveTab.IsChecked = false;
        }

        private void ArchiveTab_Click(object sender, RoutedEventArgs e)
        {
            ChatsScrollViewer.Visibility = Visibility.Collapsed;
            GroupsScrollViewer.Visibility = Visibility.Collapsed;
            ArchiveScrollViewer.Visibility = Visibility.Visible;
            ChatBoxControl.Visibility = Visibility.Visible;
            GroupChatBoxControl.Visibility = Visibility.Collapsed;
            GroupsTab.IsChecked = false;
            ChatsTab.IsChecked = false;
        }

        private async Task LoadArchivedChatsAsync()
        {
            try
            {
                var chats = await _context.Chats
                    .Include(c => c.FirstUser)
                    .Include(c => c.SecondUser)
                    .Include(c => c.Messages)
                    .Where(c => (c.FirstUserId == _currentUser.Id
                              || c.SecondUserId == _currentUser.Id)
                             && c.IsArchived)
                    .ToListAsync();

                ArchivedChatList.Clear();
                foreach (var chat in chats)
                {
                    var other = chat.FirstUserId == _currentUser.Id
                                 ? chat.SecondUser : chat.FirstUser;
                    var lastMsg = chat.Messages
                        .Where(cm => !cm.IsDeleted)
                        .OrderByDescending(m => m.SentAt).FirstOrDefault();

                    ArchivedChatList.Add(new ChatItemData
                    {
                        UserName = other.FullName,
                        UserCode = other.Code,
                        UserId = other.Id,
                        LastMessage = string.IsNullOrEmpty(lastMsg?.Message)
                                          ? "📎 مرفق"
                                          : lastMsg?.Message ?? "لا توجد رسائل",
                        LastMessageTime = lastMsg?.SentAt ?? DateTime.Now,
                        ProfileImageData = other.ProfileImageData
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadArchived error: {ex.Message}");
            }
        }

        private async Task ArchiveChatAsync(int otherUserId)
        {
            var chat = await _context.Chats.FirstOrDefaultAsync(c =>
                (c.FirstUserId == _currentUser.Id && c.SecondUserId == otherUserId) ||
                (c.FirstUserId == otherUserId && c.SecondUserId == _currentUser.Id));
            if (chat == null) return;

            chat.IsArchived = true;
            await _context.SaveChangesAsync();

            var item = ChatList.FirstOrDefault(c => c.UserId == otherUserId);
            if (item != null) ChatList.Remove(item);

            if (SelectedUserId == otherUserId)
                ChatBoxControl.ClearChat();

            await LoadArchivedChatsAsync();
        }

        private async Task UnarchiveChatAsync(int otherUserId)
        {
            var chat = await _context.Chats.FirstOrDefaultAsync(c =>
                (c.FirstUserId == _currentUser.Id && c.SecondUserId == otherUserId) ||
                (c.FirstUserId == otherUserId && c.SecondUserId == _currentUser.Id));
            if (chat == null) return;

            chat.IsArchived = false;
            await _context.SaveChangesAsync();

            var item = ArchivedChatList.FirstOrDefault(c => c.UserId == otherUserId);
            if (item != null) ArchivedChatList.Remove(item);

            await LoadChatsAsync();
        }

        private async void ArchiveChat_Click(object sender, RoutedEventArgs e)
        {
            ChatItemData item = null;

            if (sender is MenuItem menuItem)
            {
                item = menuItem.Tag as ChatItemData;

                if (item == null && menuItem.Parent is ContextMenu contextMenu)
                {
                    item = (contextMenu.PlacementTarget as Button)?.Tag as ChatItemData;
                }
            }

            if (item == null) return;
            await ArchiveChatAsync(item.UserId);
        }

        private async void UnarchiveChat_Click(object sender, RoutedEventArgs e)
        {
            ChatItemData item = null;

            if (sender is MenuItem menuItem)
            {
                item = menuItem.Tag as ChatItemData;
                if (item == null && menuItem.Parent is ContextMenu contextMenu)
                {
                    item = (contextMenu.PlacementTarget as Button)?.Tag as ChatItemData;
                }
            }

            if (item == null) return;
            await UnarchiveChatAsync(item.UserId);
        }

        private void ArchivedChatItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as ChatItemData;
            if (item == null) return;
            SelectedUserId = item.UserId;
            ChatBoxControl.LoadChat(item.UserName, item.UserCode,
                                    item.ProfileImageData, item.UserId);
            _ = MarkMessagesAsReadAsync(item.UserId);
        }

        // ── Groups ────────────────────────────────────────────────────────────────

        private async Task LoadGroupsAsync()
        {
            try
            {
                var memberships = await _context.ChatGroupMembers
                    .Include(m => m.Group)
                        .ThenInclude(g => g.Messages)
                    .Where(m => m.UserId == _currentUser.Id && m.Group.IsActive)
                    .ToListAsync();

                GroupList.Clear();
                foreach (var ms in memberships)
                {
                    var lastMsg = ms.Group.Messages?
                        .Where(m => !m.IsDeleted)
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefault();

                    GroupList.Add(new GroupItemData
                    {
                        GroupId = ms.Group.Id,
                        GroupName = ms.Group.Name,
                        GroupImageData = ms.Group.GroupImageData,
                        LastMessage = string.IsNullOrEmpty(lastMsg?.Message)
                                         ? "📎 مرفق"
                                         : lastMsg?.Message ?? "لا توجد رسائل",
                        LastMessageTime = lastMsg?.SentAt ?? ms.Group.CreatedAt,
                        UnreadCount = ms.UnreadCount,
                        IsAdmin = ms.IsAdmin
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadGroups error: {ex.Message}");
            }
        }

        private async void ShowCreateGroupDialog(object sender, RoutedEventArgs e)
        {
            var win = new CreateGroupWindow(_currentUser.Id);
            win.Owner = this;
            if (win.ShowDialog() == true)
                await LoadGroupsAsync();
        }

        private void GroupItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as GroupItemData;
            if (item == null) return;

            item.UnreadCount = 0;
            GroupChatBoxControl.LoadGroup(
                item.GroupId, item.GroupName, item.GroupImageData);

            // Reset group unread in DB
            _ = ResetGroupUnreadInDbAsync(item.GroupId);
        }

        private async Task ResetGroupUnreadInDbAsync(int groupId)
        {
            try
            {
                using var ctx = new AppDbContext(App.ConnectionString);
                var member = await ctx.ChatGroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == groupId
                                           && m.UserId == App.CurrentUser.Id);
                if (member != null && member.UnreadCount > 0)
                {
                    member.UnreadCount = 0;
                    await ctx.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ResetGroupUnread error: {ex.Message}");
            }
        }

        private void SetupGroupSignalRListener()
        {
            SignalRManager.Instance.OnGroupMessageReceived += HandleGroupMessage;
        }

        private async void HandleGroupMessage(int groupId, int senderId,
                               string message, DateTime timestamp,
                               string senderName)
        {
            if (senderId == _currentUser.Id) return;

            // ✅ FIX: Check if the group is currently open by checking SelectedGroupId
            // and whether the Groups tab is active (GroupsScrollViewer is visible)
            bool groupIsOpen = GroupChatBoxControl.SelectedGroupId == groupId
                               && GroupsScrollViewer.Visibility == Visibility.Visible;

            // Update group item's last message immediately
            var groupItem = GroupList.FirstOrDefault(g => g.GroupId == groupId);
            if (groupItem != null)
            {
                groupItem.LastMessage = string.IsNullOrEmpty(message) ? "📎 مرفق" : message;
                groupItem.LastMessageTime = timestamp;
                if (!groupIsOpen)
                    groupItem.UnreadCount++;

                // Force UI refresh
                var index = GroupList.IndexOf(groupItem);
                if (index > 0) // Only move if not already at top
                {
                    GroupList.RemoveAt(index);
                    GroupList.Insert(0, groupItem); // Move to top
                }
            }

            if (!groupIsOpen)
            {
                using var ctx = new AppDbContext(App.ConnectionString);
                var group = await ctx.ChatGroups.FindAsync(groupId);

                var displayName = !string.IsNullOrEmpty(senderName)
                    ? senderName
                    : (await ctx.Users.FindAsync(senderId))?.FullName ?? "مستخدم";

                var shortMsg = string.IsNullOrEmpty(message) ? "📎 مرفق"
                    : (message.Length > 50 ? message[..50] + "..." : message);

                Helpers.NotificationsHelper.ShowPopupNotification(
                    $"{group?.Name ?? "جروب"}: {displayName}",
                    shortMsg, this,
                    () =>
                    {
                        GroupsTab_Click(null, null);
                        var item = GroupList.FirstOrDefault(g => g.GroupId == groupId);
                        if (item != null)
                        {
                            item.UnreadCount = 0;
                            GroupChatBoxControl.LoadGroup(
                                item.GroupId, item.GroupName, item.GroupImageData);
                        }
                    });
                Helpers.NotificationsHelper.PlayNotificationSound();
            }
        }

        private void ChangeBackground_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "اختر خلفية للشات"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(dialog.FileName);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();


                // حفظ المسار في Settings عشان يتذكره
                HR_Application.Properties.Settings.Default.ChatBackgroundPath = dialog.FileName;
                HR_Application.Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ChangeBackground error: {ex.Message}");
            }
        }


        // In OnClosed
        protected override void OnClosed(EventArgs e)
        {
            SignalRManager.Instance.OnGroupMessageReceived -= HandleGroupMessage;
            ChatBoxControl.NewMessageReceived -= ChatBoxControl_NewMessageReceived;
            ChatBoxControl.NewMessageSent -= ChatBoxControl_NewMessageSent;
            ChatBoxControl.MessageUpdated -= ChatBoxControl_MessageUpdated;
            GroupChatBoxControl.GroupMessageUpdated -= GroupChatBoxControl_GroupMessageUpdated;
            base.OnClosed(e);
        }

        public ChatWindow(User currentUser) : this()
        {
            _currentUser = currentUser;
            App.CurrentUser = currentUser;

            _ = RegisterUserWithSignalR();
        }

        private void ChatBoxControl_NewMessageReceived(object sender, NewMessageEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                bool chatIsOpen = SelectedUserId == e.FromUserId;

                if (!chatIsOpen)
                {
                    await IncrementUnreadCountInDbAsync(e.FromUserId);
                }

                if (!_unreadMessagesCount.ContainsKey(e.FromUserId))
                    _unreadMessagesCount[e.FromUserId] = 0;

                if (!chatIsOpen)
                    _unreadMessagesCount[e.FromUserId]++;

                var chat = ChatList.FirstOrDefault(c => c.UserId == e.FromUserId);
                if (chat != null)
                {
                    chat.LastMessage = string.IsNullOrEmpty(e.Message) ? "📎 مرفق" : e.Message;
                    chat.LastMessageTime = e.Timestamp;
                    chat.UnreadCount = chatIsOpen ? 0 : _unreadMessagesCount[e.FromUserId];
                    MoveChatToTop(chat);
                }
                else
                {
                    await AddNewChatFromUser(e.FromUserId, e.Message, e.Timestamp);
                    var newChat = ChatList.FirstOrDefault(c => c.UserId == e.FromUserId);
                    if (newChat != null)
                        newChat.UnreadCount = chatIsOpen ? 0 : 1;
                }

                if (!chatIsOpen)
                {
                    var chatItem = ChatList.FirstOrDefault(c => c.UserId == e.FromUserId);
                    var userName = chatItem?.UserName ?? "مستخدم";
                    var shortMessage = string.IsNullOrEmpty(e.Message)
                        ? "📎 مرفق"
                        : (e.Message.Length > 50 ? e.Message[..50] + "..." : e.Message);

                    Helpers.NotificationsHelper.ShowPopupNotification(
                        $"رسالة جديدة من {userName}",
                        shortMessage,
                        this,
                        () => OpenSpecificChat(e.FromUserId)
                    );
                    Helpers.NotificationsHelper.PlayNotificationSound();
                }
            });
        }

        private async Task IncrementUnreadCountInDbAsync(int fromUserId)
        {
            try
            {
                using var context = new AppDbContext(App.ConnectionString);

                var chat = await context.Chats.FirstOrDefaultAsync(c =>
                    (c.FirstUserId == _currentUser.Id && c.SecondUserId == fromUserId) ||
                    (c.FirstUserId == fromUserId && c.SecondUserId == _currentUser.Id));

                if (chat == null) return;

                var status = await context.ChatUserStatuses
                    .FirstOrDefaultAsync(s => s.ChatId == chat.Id && s.UserId == _currentUser.Id);

                if (status == null)
                {
                    context.ChatUserStatuses.Add(new ChatUserStatus
                    {
                        ChatId = chat.Id,
                        UserId = _currentUser.Id,
                        UnreadCount = 1
                    });
                }
                else
                {
                    status.UnreadCount++;
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IncrementUnreadCount error: {ex.Message}");
            }
        }

        public void OpenSpecificChat(int userId)
        {
            var chat = ChatList.FirstOrDefault(c => c.UserId == userId);
            if (chat != null)
            {
                SelectedUserId = userId;
                ChatBoxControl.LoadChat(
                    chat.UserName, chat.UserCode,
                    chat.ProfileImageData, chat.UserId);

                _unreadMessagesCount[userId] = 0;
                chat.UnreadCount = 0;

                _ = ResetUnreadCountInDbAsync(userId);
                _ = MarkMessagesAsReadAsync(userId);
            }
        }

        private void ChatBoxControl_NewMessageSent(object sender, NewMessageEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var chat = ChatList.FirstOrDefault(c => c.UserId == e.ToUserId);
                if (chat != null)
                {
                    chat.LastMessage = e.Message;
                    chat.LastMessageTime = e.Timestamp;
                    MoveChatToTop(chat);
                }
            });
        }

        private void MoveChatToTop(ChatItemData chat)
        {
            ChatList.Remove(chat);
            ChatList.Insert(0, chat);
        }

        private async Task AddNewChatFromUser(int userId, string lastMessage, DateTime timestamp)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    var newChat = new ChatItemData
                    {
                        UserId = user.Id,
                        UserName = user.FullName,
                        UserCode = user.Code,
                        LastMessage = lastMessage,
                        LastMessageTime = timestamp,
                        ProfileImageData = user.ProfileImageData
                    };

                    ChatList.Insert(0, newChat);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding new chat: {ex.Message}");
            }
        }

        private async Task RegisterUserWithSignalR()
        {
            if (App.SignalRConnection != null && App.SignalRConnection.State == HubConnectionState.Connected)
            {
                if (App.CurrentUser != null && App.CurrentUser.Id > 0)
                {
                    await App.SignalRConnection.InvokeAsync("SetUserIdentifier", App.CurrentUser.Id.ToString());
                }
            }
        }

        private async Task LoadChatsAsync()
        {
            try
            {
                allUsers = await _context.Users
                    .Where(u => u.Id != _currentUser.Id)
                    .Take(10)
                    .ToListAsync();

                SearchResults.Clear();
                foreach (var user in allUsers)
                {
                    SearchResults.Add(new UserSearchResult
                    {
                        UserId = user.Id,
                        UserName = user.FullName,
                        UserCode = user.Code,
                        ProfileImageData = user.ProfileImageData
                    });
                }

                var chats = await _context.Chats
                    .Include(c => c.FirstUser)
                    .Include(c => c.SecondUser)
                    .Include(c => c.Messages)
                    .Where(c => (c.FirstUserId == _currentUser.Id
                      || c.SecondUserId == _currentUser.Id)
                     && !c.IsArchived)
                    .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.SentAt))
                    .ToListAsync();

                var chatIds = chats.Select(c => c.Id).ToList();
                var unreadStatuses = await _context.ChatUserStatuses
                    .Where(s => s.UserId == _currentUser.Id && chatIds.Contains(s.ChatId))
                    .ToListAsync();

                ChatList.Clear();
                foreach (var chat in chats)
                {
                    var otherUser = chat.FirstUserId == _currentUser.Id
                        ? chat.SecondUser
                        : chat.FirstUser;

                    var lastMessage = chat.Messages
                        .Where(cm => !cm.IsDeleted)
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefault();

                    string lastMessageText;
                    if (lastMessage == null)
                        lastMessageText = "لا توجد رسائل بعد";
                    else if (string.IsNullOrEmpty(lastMessage.Message))
                        lastMessageText = "📎 مرفق";
                    else
                        lastMessageText = lastMessage.Message;

                    var unreadStatus = unreadStatuses
                        .FirstOrDefault(s => s.ChatId == chat.Id);
                    var unreadCount = unreadStatus?.UnreadCount ?? 0;

                    if (unreadCount > 0)
                        _unreadMessagesCount[otherUser.Id] = unreadCount;

                    ChatList.Add(new ChatItemData
                    {
                        UserName = otherUser.FullName,
                        UserCode = otherUser.Code,
                        UserId = otherUser.Id,
                        LastMessage = lastMessageText,
                        LastMessageTime = lastMessage?.SentAt ?? DateTime.Now,
                        ProfileImageData = otherUser.ProfileImageData,
                        UnreadCount = unreadCount
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"خطأ في تحميل المحادثات: {ex.InnerException?.Message ?? ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ChatItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var chatItem = button?.Tag as ChatItemData;
            if (chatItem == null) return;

            SelectedUserId = chatItem.UserId;

            // Reset unread for this specific chat
            if (_unreadMessagesCount.ContainsKey(chatItem.UserId))
                _unreadMessagesCount[chatItem.UserId] = 0;
            chatItem.UnreadCount = 0;

            ChatBoxControl.LoadChat(
                chatItem.UserName, chatItem.UserCode,
                chatItem.ProfileImageData, chatItem.UserId);

            _ = ResetUnreadCountInDbAsync(chatItem.UserId);
            _ = MarkMessagesAsReadAsync(chatItem.UserId);
        }

        private async Task ResetUnreadCountInDbAsync(int otherUserId)
        {
            try
            {
                using var context = new AppDbContext(App.ConnectionString);

                var chat = await context.Chats.FirstOrDefaultAsync(c =>
                    (c.FirstUserId == _currentUser.Id && c.SecondUserId == otherUserId) ||
                    (c.FirstUserId == otherUserId && c.SecondUserId == _currentUser.Id));

                if (chat == null) return;

                var status = await context.ChatUserStatuses
                    .FirstOrDefaultAsync(s => s.ChatId == chat.Id && s.UserId == _currentUser.Id);

                if (status != null && status.UnreadCount > 0)
                {
                    status.UnreadCount = 0;
                    status.LastReadAt = DateTime.Now;
                    await context.SaveChangesAsync();

                    // FIX BUG #2: Notify MainWindow about unread count change
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        SignalRManager.Instance.ResetUnreadCountAsync(chat.Id, _currentUser.Id));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ResetUnreadCount error: {ex.Message}");
            }
        }

        private async Task MarkMessagesAsReadAsync(int otherUserId)
        {
            try
            {
                var chat = await _context.Chats
                    .FirstOrDefaultAsync(c =>
                        (c.FirstUserId == _currentUser.Id && c.SecondUserId == otherUserId) ||
                        (c.FirstUserId == otherUserId && c.SecondUserId == _currentUser.Id));

                if (chat != null)
                {
                    var unreadMessages = await _context.ChatMessages
                        .Where(m => m.ChatId == chat.Id && m.ReceiverId == _currentUser.Id && !m.IsRead)
                        .ToListAsync();

                    foreach (var msg in unreadMessages)
                    {
                        msg.IsRead = true;
                        msg.ReadAt = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking messages as read: {ex.Message}");
            }
        }

        private void SearchUser_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _ = SearchUsersAsync((sender as TextBox)?.Text);
            }
        }

        private async Task SearchUsersAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                SearchResults.Clear();
                return;
            }

            try
            {
                var users = await _context.Users
                    .Where(u => u.FullName.Contains(searchText) || u.Code == searchText)
                    .Where(u => u.Id != _currentUser.Id)
                    .Take(10)
                    .ToListAsync();

                SearchResults.Clear();
                foreach (var user in users)
                {
                    SearchResults.Add(new UserSearchResult
                    {
                        UserId = user.Id,
                        UserName = user.FullName,
                        UserCode = user.Code,
                        ProfileImageData = user.ProfileImageData
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddChatButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = SearchResults.FirstOrDefault();
            if (selectedUser == null)
            {
                MessageBox.Show("يرجى اختيار مستخدم أولاً", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var existingChat = await _context.Chats
                    .FirstOrDefaultAsync(c =>
                        (c.FirstUserId == _currentUser.Id && c.SecondUserId == selectedUser.UserId) ||
                        (c.FirstUserId == selectedUser.UserId && c.SecondUserId == _currentUser.Id));

                if (existingChat != null)
                {
                    MessageBox.Show("المحادثة موجودة بالفعل", "معلومات",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    var chatItem = new ChatItemData
                    {
                        UserName = selectedUser.UserName,
                        UserCode = selectedUser.UserCode,
                        UserId = selectedUser.UserId,
                        LastMessage = "ابدأ المحادثة الآن",
                        ProfileImageData = selectedUser.ProfileImageData
                    };

                    if (!ChatList.Any(c => c.UserId == selectedUser.UserId))
                        ChatList.Add(chatItem);
                }
                else
                {
                    var newChat = new Chat
                    {
                        FirstUserId = _currentUser.Id,
                        SecondUserId = selectedUser.UserId,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    _context.Chats.Add(newChat);
                    await _context.SaveChangesAsync();

                    ChatList.Add(new ChatItemData
                    {
                        UserName = selectedUser.UserName,
                        UserCode = selectedUser.UserCode,
                        UserId = selectedUser.UserId,
                        LastMessage = "محادثة جديدة",
                        ProfileImageData = selectedUser.ProfileImageData
                    });

                    MessageBox.Show("تم إنشاء المحادثة بنجاح", "تم",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                addChatGrid.Visibility = Visibility.Collapsed;
                newChatCodeBox.Text = "";
                newChatUserBox.Text = "";
                SearchResults.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء المحادثة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowAddChatDialog(object sender, RoutedEventArgs e)
        {
            foreach (var user in allUsers)
            {
                SearchResults.Add(new UserSearchResult
                {
                    UserId = user.Id,
                    UserName = user.FullName,
                    UserCode = user.Code,
                    ProfileImageData = user.ProfileImageData
                });
            }
            addChatGrid.Visibility = Visibility.Visible;
            newChatCodeBox.Focus();
        }

        private void CloseAddChatDialog(object sender, RoutedEventArgs e)
        {
            addChatGrid.Visibility = Visibility.Collapsed;
            newChatCodeBox.Text = "";
            newChatUserBox.Text = "";
            SearchResults.Clear();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void CloseCurrentChatBtn_Click(object sender, RoutedEventArgs e)
        {
            ChatBoxControl.ClearChat();
            GroupChatBoxControl.ClearGroup();
            _selectedUserId = -1;
        }
    }

    public class ChatItemData : INotifyPropertyChanged
    {
        private string _userName;
        private string _userCode;
        private int _userId;
        private string _lastMessage;
        private DateTime _lastMessageTime;
        private byte[] _profileImageData;  // تغيير من BitmapImage إلى byte[]
        private int _unreadCount;

        public int UnreadCount
        {
            get => _unreadCount;
            set
            {
                _unreadCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowUnreadBadge));
                OnPropertyChanged(nameof(UnreadCountText));
            }
        }

        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(); }
        }

        public string UserCode
        {
            get => _userCode;
            set { _userCode = value; OnPropertyChanged(); }
        }

        public int UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        public string LastMessage
        {
            get => _lastMessage;
            set { _lastMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastMessageTime)); }
        }

        public DateTime LastMessageTime
        {
            get => _lastMessageTime;
            set { _lastMessageTime = value; OnPropertyChanged(); }
        }

        public byte[] ProfileImageData
        {
            get => _profileImageData;
            set { _profileImageData = value; OnPropertyChanged(); }
        }

        public BitmapImage ProfileImageSource
        {
            get
            {
                if (ProfileImageData != null && ProfileImageData.Length > 0)
                {
                    using (MemoryStream stream = new MemoryStream(ProfileImageData))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.DecodePixelWidth = 200; // تحجيم الصورة لتحسين الأداء
                        bitmap.EndInit();
                        bitmap.Freeze(); // مهم للعمليات متعددة الخيوط

                        return bitmap;
                    }

                }
                else
                {
                    return new BitmapImage(new Uri("/assets/images/avatar.jpg", UriKind.Relative));

                }
            }
        }


        public bool ShowUnreadBadge => UnreadCount > 0;
        public string UnreadCountText => UnreadCount > 99 ? "99+" : UnreadCount.ToString();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


    public class UserSearchResult : INotifyPropertyChanged
    {
        private string _userName;
        private string _userCode;
        private int _userId;
        private byte[] _profileImageData;  // تغيير من BitmapImage إلى byte[]

        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(); }
        }

        public string UserCode
        {
            get => _userCode;
            set { _userCode = value; OnPropertyChanged(); }
        }

        public int UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        public byte[] ProfileImageData
        {
            get => _profileImageData;
            set { _profileImageData = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class GroupItemData : INotifyPropertyChanged
    {
        private int _groupId;
        private string _groupName;
        private byte[] _groupImageData;
        private string _lastMessage;
        private DateTime _lastMessageTime;
        private int _unreadCount;
        private bool _isAdmin;

        public int GroupId
        {
            get => _groupId;
            set { _groupId = value; OnPropertyChanged(); }
        }

        public string GroupName
        {
            get => _groupName;
            set { _groupName = value; OnPropertyChanged(); }
        }

        public byte[] GroupImageData
        {
            get => _groupImageData;
            set
            {
                _groupImageData = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GroupImageSource));
            }
        }

        public BitmapImage GroupImageSource
        {
            get
            {
                if (GroupImageData != null && GroupImageData.Length > 0)
                {
                    using (var stream = new System.IO.MemoryStream(GroupImageData))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.DecodePixelWidth = 60;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
                return new BitmapImage(new Uri("/assets/images/group_avatar.jpg", UriKind.Relative));
            }
        }

        public string LastMessage
        {
            get => _lastMessage;
            set { _lastMessage = value; OnPropertyChanged(); }
        }

        public DateTime LastMessageTime
        {
            get => _lastMessageTime;
            set { _lastMessageTime = value; OnPropertyChanged(); }
        }

        public int UnreadCount
        {
            get => _unreadCount;
            set
            {
                _unreadCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowUnreadBadge));
                OnPropertyChanged(nameof(UnreadCountText));
            }
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set { _isAdmin = value; OnPropertyChanged(); }
        }

        public bool ShowUnreadBadge => UnreadCount > 0;
        public string UnreadCountText => UnreadCount > 99 ? "99+" : UnreadCount.ToString();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}