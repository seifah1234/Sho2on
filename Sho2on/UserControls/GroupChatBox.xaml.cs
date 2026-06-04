using HR_Application.Services;
using HR_Application.Views.Conversations;
using Microsoft.AspNetCore.SignalR.Client;
using HR_Application.Helpers;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace HR_Application.UserControls
{
        public partial class GroupChatBox : UserControl, INotifyPropertyChanged
        {
            public int SelectedGroupId { get; private set; }
            public bool CurrentUserIsAdmin { get; private set; }

            public ObservableCollection<UIChatMessage> Messages { get; set; }
            public ObservableCollection<ChatAttachmentItem> SelectedAttachments { get; set; }

            private List<ChatAttachmentItem> _pendingAttachments = new();
            private bool _signalRInitialized = false;
            private int _editingMessageId = -1;

            public event EventHandler<GroupMessageEventArgs> NewGroupMessageReceived;
            public event EventHandler<GroupMessageUpdatedEventArgs> GroupMessageUpdated;
            public event PropertyChangedEventHandler PropertyChanged;

            public GroupChatBox()
            {
                InitializeComponent();
                Messages = new ObservableCollection<UIChatMessage>();
                SelectedAttachments = new ObservableCollection<ChatAttachmentItem>();
                MessagesItemsControl.ItemsSource = Messages;
                DataContext = this;
                Loaded += (s, e) => SetupSignalRListener();
                LoadSavedBackground();
            }

            public void AddIncomingGroupMessage(GroupMessageEventArgs e)
            {
                Dispatcher.Invoke(async () =>
                {
                    if (e.GroupId != SelectedGroupId) return;
                    if (e.FromUserId == App.CurrentUser.Id) return;

                    Messages.Add(new UIChatMessage
                    {
                        MessageText = e.Message,
                        IsFromMe = false,
                        SenderName = e.SenderName ?? "",
                        Time = e.Timestamp.ToString("hh:mm tt"),
                        SentAt = e.Timestamp,
                        IsDelivered = true,
                        IsRead = true,
                        MessageDbId = e.MessageId // FIX: Store message ID
                    });

                    ScrollToBottom();
                    await ResetUnreadCountAsync(SelectedGroupId);

                    // FIX BUG #3: Notify parent about updated last message
                    GroupMessageUpdated?.Invoke(this, new GroupMessageUpdatedEventArgs
                    {
                        GroupId = SelectedGroupId,
                        LastMessage = GetLastMessageText(),
                        LastMessageTime = GetLastMessageTime(),
                        UpdateType = "NewMessage"
                    });
                });
            }

            // ?? Load ?????????????????????????????????????????????????????????????

            public async void LoadGroup(int groupId, string groupName,
                                        byte[] groupImage = null)
            {
                SelectedGroupId = groupId;
                GroupNameText.Text = groupName;


                await SignalRManager.Instance.JoinGroupAsync(groupId);

                if (groupImage?.Length > 0)
                    GroupImage.Source = ConvertToImage(groupImage);

                NoChatSelectedPlaceholder.Visibility = Visibility.Collapsed;
                MessageTextBox.IsEnabled = true;
                SendButton.IsEnabled = true;

                Messages.Clear();
                await LoadMessagesAsync(groupId);
                await MarkMessagesAsReadAsync(groupId);
                await SendReadReceiptAsync(groupId);
                await LoadMembersInfoAsync(groupId);
                await ResetUnreadCountAsync(groupId);
            }

            private async Task MarkMessagesAsReadAsync(int groupId)
            {
                try
                {
                    using var ctx = new AppDbContext(App.ConnectionString);

                    // جيب الرسائل اللي لسه ما قرأتهاش
                    var readMessageIds = await ctx.ChatGroupMessageReads
                        .Where(r => r.UserId == App.CurrentUser.Id)
                        .Select(r => r.MessageId)
                        .ToListAsync();

                    var unreadMessages = await ctx.ChatGroupMessages
                        .Where(m => m.GroupId == groupId
                                 && !m.IsDeleted
                                 && m.SenderId != App.CurrentUser.Id
                                 && !readMessageIds.Contains(m.Id))
                        .ToListAsync();

                    if (!unreadMessages.Any()) return;

                    foreach (var msg in unreadMessages)
                    {
                        ctx.ChatGroupMessageReads.Add(new ChatGroupMessageRead
                        {
                            MessageId = msg.Id,
                            UserId = App.CurrentUser.Id,
                            ReadAt = DateTime.Now
                        });
                    }
                    await ctx.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MarkGroupRead error: {ex.Message}");
                }
            }

            private async Task SendReadReceiptAsync(int groupId)
            {
                try
                {
                    if (App.SignalRConnection?.State == HubConnectionState.Connected)
                    {
                        await App.SignalRConnection.InvokeAsync(
                            "MarkGroupMessagesRead", groupId, App.CurrentUser.Id);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SendGroupReadReceipt error: {ex.Message}");
                }
            }

            public void ClearGroup()
            {
                if (SelectedGroupId > 0)
                {
                    _ = SignalRManager.Instance.LeaveGroupAsync(SelectedGroupId);
                }
                SelectedGroupId = 0;
                CurrentUserIsAdmin = false;
                Messages.Clear();
                NoChatSelectedPlaceholder.Visibility = Visibility.Visible;
                MessageTextBox.IsEnabled = false;
                SendButton.IsEnabled = false;
                ManageMembersBtn.Visibility = Visibility.Collapsed;
            }

            private async Task LoadMessagesAsync(int groupId)
            {
                try
                {
                    using var ctx = new AppDbContext(App.ConnectionString);
                    var messages = await ctx.ChatGroupMessages
                        .Include(m => m.Sender)
                        .Include(m => m.Attachments)
                        .Where(m => m.GroupId == groupId && !m.IsDeleted)
                        .OrderBy(m => m.SentAt)
                        .ToListAsync();

                    foreach (var msg in messages)
                    {
                        var uiMsg = new UIChatMessage
                        {
                            MessageDbId = msg.Id, // FIX: Store message ID
                            MessageText = msg.Message ?? "",
                            IsFromMe = msg.SenderId == App.CurrentUser.Id,
                            SenderName = msg.SenderId == App.CurrentUser.Id
                                         ? "" : msg.Sender?.FullName ?? "",
                            Time = msg.SentAt.ToString("hh:mm tt"),
                            SentAt = msg.SentAt,
                            IsDelivered = true,
                            IsRead = true,
                            IsEdited = msg.IsEdited
                        };

                        foreach (var att in msg.Attachments
                                              ?? Enumerable.Empty<ChatGroupAttachment>())
                        {
                            uiMsg.Attachments.Add(new ChatAttachmentItem
                            {
                                FileName = att.FileName,
                                FileSize = att.FileSize,
                                FileData = att.FileData,
                                FileIcon = GetFileIcon(Path.GetExtension(att.FileName))
                            });
                        }

                        Messages.Add(uiMsg);
                    }

                    ScrollToBottom();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LoadGroupMessages error: {ex.Message}");
                }
            }

            private async Task LoadMembersInfoAsync(int groupId)
            {
                try
                {
                    using var ctx = new AppDbContext(App.ConnectionString);
                    var membersCount = await ctx.ChatGroupMembers
                        .CountAsync(m => m.GroupId == groupId);

                    MembersCountText.Text = $"{membersCount} عضو";

                    var myMembership = await ctx.ChatGroupMembers
                        .FirstOrDefaultAsync(m => m.GroupId == groupId
                                               && m.UserId == App.CurrentUser.Id);

                    CurrentUserIsAdmin = myMembership?.IsAdmin ?? false;
                    ManageMembersBtn.Visibility = CurrentUserIsAdmin
                        ? Visibility.Visible : Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LoadMembersInfo error: {ex.Message}");
                }
            }

        // ?? Send ?????????????????????????????????????????????????????????????

        private async void SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) && _pendingAttachments.Count == 0)
                return;
            if (SelectedGroupId == 0) return;

            // Handle edit mode
            if (_editingMessageId != -1)
            {
                await SaveGroupEditAsync(_editingMessageId, message);
                return;
            }

            var tempMsg = new UIChatMessage
            {
                MessageDbId = 0,
                MessageText = message ?? "",
                IsFromMe = true,
                SenderName = App.CurrentUser.FullName,
                Time = DateTime.Now.ToString("hh:mm tt"),
                SentAt = DateTime.Now,
                IsDelivered = true,
                IsRead = false
            };

            foreach (var a in _pendingAttachments)
                tempMsg.Attachments.Add(a);

            Messages.Add(tempMsg);
            MessageTextBox.Text = "";

            var attachments = _pendingAttachments.ToList();
            _pendingAttachments.Clear();
            SelectedAttachments.Clear();
            AttachmentsScrollViewer.Visibility = Visibility.Collapsed;
            ScrollToBottom();

            await SendToServerAsync(message ?? "", attachments);
        }

        // FIX BUG #4: Save edit for group messages
        private async Task SaveGroupEditAsync(int messageId, string newText)
        {
            try
            {
                Console.WriteLine($"GroupChatBox: Editing message {messageId} in group {SelectedGroupId}");

                using var ctx = new AppDbContext(App.ConnectionString);
                var dbMsg = await ctx.ChatGroupMessages.FindAsync(messageId);

                if (dbMsg == null || dbMsg.SenderId != App.CurrentUser.Id)
                {
                    LocalizationManager.ShowMessage("لا يمكن تعديل هذه الرسالة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                dbMsg.Message = newText;
                dbMsg.IsEdited = true;
                dbMsg.EditedAt = DateTime.Now;
                await ctx.SaveChangesAsync();

                // Update UI
                var uiMsg = Messages.FirstOrDefault(m => m.MessageDbId == messageId);
                if (uiMsg != null)
                {
                    uiMsg.MessageText = newText;
                    uiMsg.IsEdited = true;

                    // Force UI refresh
                    var index = Messages.IndexOf(uiMsg);
                    if (index >= 0)
                    {
                        Messages.RemoveAt(index);
                        Messages.Insert(index, uiMsg);
                    }

                    // Check if this is the last message and notify parent
                    var lastMsg = Messages.LastOrDefault();
                    if (lastMsg != null && lastMsg.MessageDbId == messageId)
                    {
                        GroupMessageUpdated?.Invoke(this, new GroupMessageUpdatedEventArgs
                        {
                            GroupId = SelectedGroupId,
                            LastMessage = GetLastMessageText(),
                            LastMessageTime = GetLastMessageTime(),
                            UpdateType = "Edit"
                        });
                    }
                }

                // ? FIXED: Send notification via SignalR
                if (App.SignalRConnection?.State == HubConnectionState.Connected)
                {
                    Console.WriteLine($"GroupChatBox: Sending GroupMessageEdited via SignalR - MsgId={messageId}, GroupId={SelectedGroupId}");
                    await App.SignalRConnection.InvokeAsync(
                        "GroupMessageEdited", messageId, SelectedGroupId, newText);
                }
                else
                {
                    Console.WriteLine("GroupChatBox: SignalR not connected, can't send edit notification");
                }

                _editingMessageId = -1;
                MessageTextBox.Text = "";
                EditBar.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EditGroupMessage error: {ex.Message}");
                LocalizationManager.ShowMessage($"خطأ في تعديل الرسالة: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SendToServerAsync(string message,
                                                  List<ChatAttachmentItem> attachments)
            {
                try
                {
                    using var ctx = new AppDbContext(App.ConnectionString);

                    // حفظ الرسالة
                    var dbMsg = new ChatGroupMessage
                    {
                        GroupId = SelectedGroupId,
                        SenderId = App.CurrentUser.Id,
                        Message = message,
                        SentAt = DateTime.Now
                    };
                    ctx.ChatGroupMessages.Add(dbMsg);
                    await ctx.SaveChangesAsync();

                    // FIX: Set message ID for UI
                    var tempMsg = Messages.LastOrDefault(m => m.IsFromMe && m.MessageDbId == 0);
                    if (tempMsg != null)
                        tempMsg.MessageDbId = dbMsg.Id;

                    // حفظ المرفقات
                    foreach (var att in attachments)
                    {
                        ctx.ChatGroupAttachments.Add(new ChatGroupAttachment
                        {
                            MessageId = dbMsg.Id,
                            FileName = att.FileName,
                            FileSize = att.FileSize,
                            FileData = att.FileData,
                            ContentType = GetContentType(att.FileName),
                            CreatedAt = DateTime.Now
                        });
                    }

                    // زوّد UnreadCount لباقي الأعضاء
                    var otherMembers = await ctx.ChatGroupMembers
                        .Where(m => m.GroupId == SelectedGroupId
                                 && m.UserId != App.CurrentUser.Id)
                        .ToListAsync();

                    foreach (var m in otherMembers)
                        m.UnreadCount++;

                    await ctx.SaveChangesAsync();

                    // SignalR

                    await SendRealTimeNotification(SelectedGroupId, App.CurrentUser.Id, message, App.CurrentUser.FullName);

                // FIX BUG #3: Notify parent about updated last message
                    GroupMessageUpdated?.Invoke(this, new GroupMessageUpdatedEventArgs
                    {
                        GroupId = SelectedGroupId,
                        LastMessage = string.IsNullOrEmpty(message) ? LocalizationManager.Translate("?? مرفق") : message,
                        LastMessageTime = DateTime.Now,
                        UpdateType = "NewMessage"
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SendGroupMessage error: {ex.Message}");
                }
            }

        private async Task SendRealTimeNotification(int groupId, int senderId, string message, string senderName)
        {
            try
            {
                if (App.SignalRConnection == null)
                {
                    return;
                }

                if (App.SignalRConnection.State != HubConnectionState.Connected)
                {
                    try
                    {
                        await App.SignalRConnection.StartAsync();

                        // إعادة تسجيل المستخدم بعد إعادة الاتصال
                        if (App.CurrentUser != null && App.CurrentUser.Id > 0)
                        {
                            await App.SignalRConnection.InvokeAsync("SetUserIdentifier", App.CurrentUser.Id.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        LocalizationManager.ShowMessage($"Reconnect failed: {ex.Message}");
                        return;
                    }
                }

                if (App.CurrentUser == null)
                {
                    return;
                }


                await App.SignalRConnection.InvokeAsync("SendGroupMessage",
                    SelectedGroupId, App.CurrentUser.Id, message, App.CurrentUser.FullName);
                // إرسال الرسالة

            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"SignalR error: {ex.Message}");
            }
        }

        // FIX BUG #4: Delete group message
        private async void DeleteGroupMessage_Click(object sender, RoutedEventArgs e)
        {
            var msg = GetGroupMessageFromContextMenu(sender);
            if (msg == null || !msg.IsFromMe) return;

            var confirm = LocalizationManager.ShowMessage("هل تريد حذف هذه الرسالة؟", LocalizationManager.Translate("تأكيد"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Console.WriteLine($"GroupChatBox: Deleting message {msg.MessageDbId} from group {SelectedGroupId}");

                using var ctx = new AppDbContext(App.ConnectionString);
                var dbMsg = await ctx.ChatGroupMessages.FindAsync(msg.MessageDbId);
                if (dbMsg != null)
                {
                    dbMsg.IsDeleted = true;
                    await ctx.SaveChangesAsync();
                }

                Messages.Remove(msg);

                // ? FIXED: Send notification via SignalR with correct method name
                if (App.SignalRConnection?.State == HubConnectionState.Connected)
                {
                    Console.WriteLine($"GroupChatBox: Sending GroupMessageDeleted via SignalR - MsgId={msg.MessageDbId}, GroupId={SelectedGroupId}");
                    await App.SignalRConnection.InvokeAsync(
                        "GroupMessageDeleted", msg.MessageDbId, SelectedGroupId);
                }
                else
                {
                    Console.WriteLine("GroupChatBox: SignalR not connected, can't send delete notification");
                }

                // Update group item's last message
                GroupMessageUpdated?.Invoke(this, new GroupMessageUpdatedEventArgs
                {
                    GroupId = SelectedGroupId,
                    LastMessage = GetLastMessageText(),
                    LastMessageTime = GetLastMessageTime(),
                    UpdateType = "Delete"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteGroupMessage error: {ex.Message}");
            }
        }

        // FIX BUG #4: Edit group message UI handler
        private async void EditGroupMessage_Click(object sender, RoutedEventArgs e)
        {
            var msg = GetGroupMessageFromContextMenu(sender);
            if (msg == null || !msg.IsFromMe) return;

            // Show edit bar
            _editingMessageId = msg.MessageDbId;
            MessageTextBox.Text = msg.MessageText;
            MessageTextBox.Focus();
            MessageTextBox.CaretIndex = MessageTextBox.Text.Length;

            EditBar.Visibility = Visibility.Visible;
            EditingLabel.Text = $"?? تعديل: {(msg.MessageText?.Length > 30 ? msg.MessageText[..30] + "..." : msg.MessageText)}";
        }

        private void CancelGroupEdit_Click(object sender, RoutedEventArgs e)
            {
                _editingMessageId = -1;
                MessageTextBox.Text = "";
                EditBar.Visibility = Visibility.Collapsed;
            }

        // Helper methods
        private UIChatMessage GetGroupMessageFromContextMenu(object sender)
        {
            if (sender is MenuItem mi && mi.Tag is UIChatMessage msg)
                return msg;

            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu cm &&
                cm.PlacementTarget is FrameworkElement element)
            {
                return element.Tag as UIChatMessage;
            }

            return null;
        }

        private string GetLastMessageText()
            {
                var lastMsg = Messages.LastOrDefault();
                if (lastMsg == null) return LocalizationManager.Translate("لا توجد رسائل");
                if (string.IsNullOrEmpty(lastMsg.MessageText)) return LocalizationManager.Translate("?? مرفق");
                return lastMsg.MessageText;
            }

            private DateTime GetLastMessageTime()
            {
                var lastMsg = Messages.LastOrDefault();
                return lastMsg?.SentAt ?? DateTime.Now;
            }

            // ?? Unread ???????????????????????????????????????????????????????????

            private async Task ResetUnreadCountAsync(int groupId)
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

                        // FIX BUG #2: Notify SignalR about unread count change
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                            SignalRManager.Instance.ResetUnreadCountAsync(groupId, App.CurrentUser.Id)
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ResetGroupUnread error: {ex.Message}");
                }
            }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            _editingMessageId = -1;
            MessageTextBox.Text = "";
            EditBar.Visibility = Visibility.Collapsed;
        }

        // ?? SignalR ??????????????????????????????????????????????????????????

        private void SetupSignalRListener()
        {
            if (_signalRInitialized) return;

            Console.WriteLine("GroupChatBox: Setting up SignalR listeners");

            SignalRManager.Instance.OnGroupMessageReceived += HandleGroupMessage;
            SignalRManager.Instance.OnGroupMessageEdited += HandleGroupMessageEdited;
            SignalRManager.Instance.OnGroupMessageDeleted += HandleGroupMessageDeleted;

            _signalRInitialized = true;

            Unloaded += (s, e) =>
            {
                Console.WriteLine("GroupChatBox: Removing SignalR listeners");
                SignalRManager.Instance.OnGroupMessageReceived -= HandleGroupMessage;
                SignalRManager.Instance.OnGroupMessageEdited -= HandleGroupMessageEdited;
                SignalRManager.Instance.OnGroupMessageDeleted -= HandleGroupMessageDeleted;
                _signalRInitialized = false;
            };
        }

        // FIX BUG #1 & #6: Handle edited group messages in real-time
        private void HandleGroupMessageEdited(int messageId, int groupId, string newText)
        {
            Console.WriteLine($"GroupChatBox: HandleGroupMessageEdited - MsgId={messageId}, GroupId={groupId}, CurrentGroupId={SelectedGroupId}");

            if (groupId != SelectedGroupId)
            {
                Console.WriteLine($"GroupChatBox: Ignoring edit for group {groupId}, current is {SelectedGroupId}");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var msg = Messages.FirstOrDefault(m => m.MessageDbId == messageId);
                if (msg != null)
                {
                    Console.WriteLine($"GroupChatBox: Found message to edit: {msg.MessageText} -> {newText}");
                    msg.MessageText = newText;
                    msg.IsEdited = true;

                    // Force UI refresh
                    var index = Messages.IndexOf(msg);
                    if (index >= 0)
                    {
                        Messages.RemoveAt(index);
                        Messages.Insert(index, msg);
                    }

                    // Check if this is the last message and notify parent
                    var lastMsg = Messages.LastOrDefault();
                    if (lastMsg != null && lastMsg.MessageDbId == messageId)
                    {
                        Console.WriteLine("GroupChatBox: Notifying parent about last message edit");
                        GroupMessageUpdated?.Invoke(this, new GroupMessageUpdatedEventArgs
                        {
                            GroupId = SelectedGroupId,
                            LastMessage = GetLastMessageText(),
                            LastMessageTime = GetLastMessageTime(),
                            UpdateType = "Edit"
                        });
                    }
                }
                else
                {
                    Console.WriteLine($"GroupChatBox: Message {messageId} not found in UI");
                }
            });
        }

        // FIX BUG #6: Handle deleted group messages in real-time
        private void HandleGroupMessageDeleted(int messageId, int groupId)
        {
            Console.WriteLine($"GroupChatBox: HandleGroupMessageDeleted - MsgId={messageId}, GroupId={groupId}, CurrentGroupId={SelectedGroupId}");

            if (groupId != SelectedGroupId)
            {
                Console.WriteLine($"GroupChatBox: Ignoring delete for group {groupId}, current is {SelectedGroupId}");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var msg = Messages.FirstOrDefault(m => m.MessageDbId == messageId);
                if (msg != null)
                {
                    Console.WriteLine($"GroupChatBox: Removing message: {msg.MessageText}");
                    Messages.Remove(msg);

                    // Update group item's last message
                    GroupMessageUpdated?.Invoke(this, new GroupMessageUpdatedEventArgs
                    {
                        GroupId = SelectedGroupId,
                        LastMessage = GetLastMessageText(),
                        LastMessageTime = GetLastMessageTime(),
                        UpdateType = "Delete"
                    });
                }
                else
                {
                    Console.WriteLine($"GroupChatBox: Message {messageId} not found in UI");
                }
            });
        }

        private async void HandleGroupMessage(int groupId, int senderId,
                               string message, DateTime timestamp,
                               string senderName)
        {
            Console.WriteLine($"GroupChatBox: HandleGroupMessage - GroupId={groupId}, SenderId={senderId}, CurrentGroupId={SelectedGroupId}");

            if (senderId == App.CurrentUser.Id) return;

            NewGroupMessageReceived?.Invoke(this, new GroupMessageEventArgs
            {
                GroupId = groupId,
                FromUserId = senderId,
                SenderName = senderName,
                Message = message,
                Timestamp = timestamp
            });

            if (groupId != SelectedGroupId) return;

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                // ? FIX: Get the message from database with attachments
                int messageId = 0;
                var attachments = new List<ChatAttachmentItem>();

                try
                {
                    using var ctx = new AppDbContext(App.ConnectionString);
                    var dbMsg = await ctx.ChatGroupMessages
                        .Include(m => m.Attachments)
                        .Where(m => m.GroupId == groupId
                                 && m.SenderId == senderId
                                 && m.Message == message
                                 && m.SentAt <= timestamp
                                 && !m.IsDeleted)
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefaultAsync();

                    if (dbMsg != null)
                    {
                        messageId = dbMsg.Id;

                        // Load attachments
                        if (dbMsg.Attachments != null)
                        {
                            foreach (var att in dbMsg.Attachments)
                            {
                                attachments.Add(new ChatAttachmentItem
                                {
                                    FileName = att.FileName,
                                    FileSize = att.FileSize,
                                    FileData = att.FileData,
                                    FileIcon = GetFileIcon(Path.GetExtension(att.FileName))
                                });
                            }
                        }

                        Console.WriteLine($"GroupChatBox: Found message ID {messageId} with {attachments.Count} attachments");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GroupChatBox: Error getting message from DB: {ex.Message}");
                }

                var uiMsg = new UIChatMessage
                {
                    MessageDbId = messageId,
                    MessageText = message,
                    IsFromMe = false,
                    SenderName = senderName,
                    Time = timestamp.ToString("hh:mm tt"),
                    SentAt = timestamp,
                    IsDelivered = true,
                    IsRead = true
                };

                // ? Add attachments to UI message
                foreach (var att in attachments)
                {
                    uiMsg.Attachments.Add(att);
                }

                Messages.Add(uiMsg);
                ScrollToBottom();
                _ = ResetUnreadCountAsync(groupId);
            });
        }

        private void LoadSavedBackground()
        {
            var path = HR_Application.Properties.Settings.Default.ChatBackgroundPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                var bitmap = new BitmapImage(new Uri(path));
                bitmap.Freeze();
                ChatBackgroundBrush.ImageSource = bitmap;
            }
            catch { }
        }

        // ?? Members Management ???????????????????????????????????????????????

        private void ManageMembers_Click(object sender, RoutedEventArgs e)
            {
                if (!CurrentUserIsAdmin) return;
                var win = new GroupMembersWindow(SelectedGroupId);
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
                _ = LoadMembersInfoAsync(SelectedGroupId);
            }

            // ?? Attachments ??????????????????????????????????????????????????????

            private void AttachButton_Click(object sender, RoutedEventArgs e)
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Multiselect = true,
                    Filter = "All files (*.*)|*.*"
                };
                if (dialog.ShowDialog() != true) return;

                foreach (var path in dialog.FileNames)
                {
                    var info = new FileInfo(path);
                    if (info.Length > 10 * 1024 * 1024)
                    {
                        LocalizationManager.ShowMessage($"الملف {info.Name} أكبر من 10MB");
                        continue;
                    }

                    var att = new ChatAttachmentItem
                    {
                        FileName = info.Name,
                        FileSize = info.Length,
                        FileData = File.ReadAllBytes(path),
                        FileIcon = GetFileIcon(info.Extension)
                    };
                    _pendingAttachments.Add(att);
                    SelectedAttachments.Add(att);
                }

                AttachmentsScrollViewer.Visibility =
                    SelectedAttachments.Count > 0
                        ? Visibility.Visible : Visibility.Collapsed;
            }

            private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
            {
                var att = (sender as Button)?.Tag as ChatAttachmentItem;
                if (att == null) return;
                _pendingAttachments.Remove(att);
                SelectedAttachments.Remove(att);
                AttachmentsScrollViewer.Visibility =
                    SelectedAttachments.Count > 0
                        ? Visibility.Visible : Visibility.Collapsed;
            }

        private async void DownloadAttachment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var attachment = button?.Tag as ChatAttachmentItem;

            if (attachment != null)
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = attachment.FileName,
                    Filter = "All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        File.WriteAllBytes(dialog.FileName, attachment.FileData);
                        LocalizationManager.ShowMessage("تم حفظ الملف بنجاح", LocalizationManager.Translate("تم"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        LocalizationManager.ShowMessage($"خطأ في حفظ الملف: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // ?? UI Helpers ???????????????????????????????????????????????????????

        private void SendButton_Click(object sender, RoutedEventArgs e) =>
                SendMessage(MessageTextBox.Text);

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                e.Handled = true;
                SendMessage(MessageTextBox.Text);
            }
        }

        private void ScrollToBottom() =>
                MessagesScrollViewer?.Dispatcher.Invoke(() =>
                    MessagesScrollViewer?.ScrollToEnd());

            private BitmapImage ConvertToImage(byte[] data)
            {
                using var stream = new MemoryStream(data);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = stream;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }

            private string GetFileIcon(string ext) => ext.ToLower() switch
            {
                ".pdf" => "FilePdf",
                ".doc" or ".docx" => "FileWord",
                ".xls" or ".xlsx" => "FileExcel",
                ".jpg" or ".jpeg"
                or ".png" or ".gif" => "FileImage",
                ".zip" or ".rar" => "FileZip",
                _ => "File"
            };

            private string GetContentType(string fileName) =>
                Path.GetExtension(fileName).ToLower() switch
                {
                    ".pdf" => "application/pdf",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".docx" => "application/vnd.openxmlformats-officedocument" +
                               ".wordprocessingml.document",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument" +
                               ".spreadsheetml.sheet",
                    _ => "application/octet-stream"
                };

            protected void OnPropertyChanged([CallerMemberName] string name = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class GroupMessageEventArgs : EventArgs
        {
            public int GroupId { get; set; }
            public int FromUserId { get; set; }
            public string Message { get; set; }
            public string SenderName { get; set; }
            public DateTime Timestamp { get; set; }
            public int MessageId { get; set; } // FIX: Add message ID
        }

        // FIX BUG #3: New event args for group message updates
        public class GroupMessageUpdatedEventArgs : EventArgs
        {
            public int GroupId { get; set; }
            public string LastMessage { get; set; }
            public DateTime LastMessageTime { get; set; }
            public string UpdateType { get; set; } // "NewMessage", "Delete", "Edit"
        }
    
}
