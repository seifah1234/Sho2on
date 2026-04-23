using DocumentFormat.OpenXml.Vml;
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

            Loaded += async (s, e) => await LoadChatsAsync();
        }

        public ChatWindow(User currentUser) : this()
        {
            _currentUser = currentUser; 
            App.CurrentUser = currentUser;  // تأكد من تعيينها

            // إعادة تسجيل المستخدم في SignalR
            _ = RegisterUserWithSignalR();

        }

        private void ChatBoxControl_NewMessageReceived(object sender, NewMessageEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                bool chatIsOpen = SelectedUserId == e.FromUserId;

                // ✅ حدّث DB لو الشات مش مفتوح
                if (!chatIsOpen)
                {
                    await IncrementUnreadCountInDbAsync(e.FromUserId);
                }

                // حدّث الـ dictionary
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

        // ✅ دالة جديدة لتحديث UnreadCount في DB
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

        private void OpenSpecificChat(int userId)
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

                _ = ResetUnreadCountInDbAsync(userId);  // ✅
                _ = MarkMessagesAsReadAsync(userId);
            }
        }

        private void ChatBoxControl_NewMessageSent(object sender, NewMessageEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {

                // تحديث الـ ChatItem للمستقبل
                var chat = ChatList.FirstOrDefault(c => c.UserId == e.ToUserId);
                if (chat != null)
                {
                    chat.LastMessage = e.Message;
                    chat.LastMessageTime = e.Timestamp;

                    // نقل المحادثة إلى الأعلى
                    MoveChatToTop(chat);
                }
            });
        }

        private void MoveChatToTop(ChatItemData chat)
        {
            // إزالة العنصر من مكانه الحالي
            ChatList.Remove(chat);
            // إضافته في البداية
            ChatList.Insert(0, chat);
        }

        private async Task AddNewChatFromUser(int userId, string lastMessage, DateTime timestamp)
        {
            try
            {
                // جلب بيانات المستخدم من قاعدة البيانات
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

        // تأكد من إلغاء الاشتراك عند إغلاق النافذة
        protected override void OnClosed(EventArgs e)
        {
            ChatBoxControl.NewMessageReceived -= ChatBoxControl_NewMessageReceived;
            ChatBoxControl.NewMessageSent -= ChatBoxControl_NewMessageSent;
            base.OnClosed(e);
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
                    .Where(c => c.FirstUserId == _currentUser.Id
                             || c.SecondUserId == _currentUser.Id)
                    .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.SentAt))
                    .ToListAsync();

                // جيب كل الـ UnreadCounts للـ currentUser دفعة واحدة
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
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefault();

                    // ✅ الشرط الصح
                    string lastMessageText;
                    if (lastMessage == null)
                        lastMessageText = "لا توجد رسائل بعد";
                    else if (string.IsNullOrEmpty(lastMessage.Message))
                        lastMessageText = "📎 مرفق";
                    else
                        lastMessageText = lastMessage.Message;

                    // ✅ جيب UnreadCount من DB
                    var unreadStatus = unreadStatuses
                        .FirstOrDefault(s => s.ChatId == chat.Id);
                    var unreadCount = unreadStatus?.UnreadCount ?? 0;

                    // sync مع الـ dictionary
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
                        UnreadCount = unreadCount  // ✅ بدل ما يبدأ بصفر
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

            _unreadMessagesCount[chatItem.UserId] = 0;
            chatItem.UnreadCount = 0;

            ChatBoxControl.LoadChat(
                chatItem.UserName, chatItem.UserCode,
                chatItem.ProfileImageData, chatItem.UserId);

            _ = ResetUnreadCountInDbAsync(chatItem.UserId);  // ✅
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
                    .Where(u => u.Id != _currentUser.Id) // استبعاد المستخدم الحالي
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
                // التحقق من وجود محادثة سابقة
                var existingChat = await _context.Chats
                    .FirstOrDefaultAsync(c =>
                        (c.FirstUserId == _currentUser.Id && c.SecondUserId == selectedUser.UserId) ||
                        (c.FirstUserId == selectedUser.UserId && c.SecondUserId == _currentUser.Id));

                if (existingChat != null)
                {
                    MessageBox.Show("المحادثة موجودة بالفعل", "معلومات",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // إضافة للمحادثات الظاهرة
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
                    // إنشاء محادثة جديدة
                    var newChat = new Chat
                    {
                        FirstUserId = _currentUser.Id,
                        SecondUserId = selectedUser.UserId,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    _context.Chats.Add(newChat);
                    await _context.SaveChangesAsync();

                    // إضافة للمحادثات الظاهرة
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

                // إغلاق نافذة الإضافة
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
            ChatBoxControl.ClearChat();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // Model لعنصر المحادثة
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

    // Model لنتائج البحث
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

}