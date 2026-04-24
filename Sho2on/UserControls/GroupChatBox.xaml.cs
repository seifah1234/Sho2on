using HR_Application.Services;
using HR_Application.Views.Conversations;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
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

        public ObservableCollection<ChatMessage> Messages { get; set; }
        public ObservableCollection<ChatAttachmentItem> SelectedAttachments { get; set; }

        private List<ChatAttachmentItem> _pendingAttachments = new();
        private bool _signalRInitialized = false;

        public event EventHandler<GroupMessageEventArgs> NewGroupMessageReceived;
        public event PropertyChangedEventHandler PropertyChanged;

        public GroupChatBox()
        {
            InitializeComponent();
            Messages = new ObservableCollection<ChatMessage>();
            SelectedAttachments = new ObservableCollection<ChatAttachmentItem>();
            MessagesItemsControl.ItemsSource = Messages;
            DataContext = this;
            Loaded += (s, e) => SetupSignalRListener();
        }

        public void AddIncomingGroupMessage(GroupMessageEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                if (e.GroupId != SelectedGroupId) return;
                if (e.FromUserId == App.CurrentUser.Id) return;

                Messages.Add(new ChatMessage
                {
                    MessageText = e.Message,
                    IsFromMe = false,
                    SenderName = e.SenderName ?? "",
                    Time = e.Timestamp.ToString("hh:mm tt"),
                    SentAt = e.Timestamp,
                    IsDelivered = true,
                    IsRead = true
                });

                ScrollToBottom();
                await ResetUnreadCountAsync(SelectedGroupId);
            });
        }

        // ── Load ─────────────────────────────────────────────────────────────

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
                    var uiMsg = new ChatMessage
                    {
                        MessageText = msg.Message ?? "",
                        IsFromMe = msg.SenderId == App.CurrentUser.Id,
                        SenderName = msg.SenderId == App.CurrentUser.Id
                                     ? "" : msg.Sender?.FullName ?? "",
                        Time = msg.SentAt.ToString("hh:mm tt"),
                        SentAt = msg.SentAt,
                        IsDelivered = true,
                        IsRead = true
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

        // ── Send ─────────────────────────────────────────────────────────────

        private async void SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) && _pendingAttachments.Count == 0)
                return;
            if (SelectedGroupId == 0) return;

            var tempMsg = new ChatMessage
            {
                MessageText = message ?? "",
                IsFromMe = true,
                SenderName = "",
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
                await App.SignalRConnection.InvokeAsync("SendGroupMessage",
                    SelectedGroupId, App.CurrentUser.Id, message, App.CurrentUser.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendGroupMessage error: {ex.Message}");
            }
        }

        // ── Unread ───────────────────────────────────────────────────────────

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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ResetGroupUnread error: {ex.Message}");
            }
        }

        // ── SignalR ──────────────────────────────────────────────────────────

        private void SetupSignalRListener()
        {
            if (_signalRInitialized || App.SignalRConnection == null)
            {
                if (App.SignalRConnection == null)
                {
                    Task.Delay(1000).ContinueWith(_ =>
                        Application.Current.Dispatcher.Invoke(SetupSignalRListener));
                }
                return;
            }

            App.SignalRConnection.Remove("ReceiveGroupMessage");
            App.SignalRConnection.On<int, int, string, DateTime, string>(
                "ReceiveGroupMessage",
                async (groupId, senderId, message, timestamp, senderName) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        // أطلع الـ event للـ ChatWindow
                        NewGroupMessageReceived?.Invoke(this, new GroupMessageEventArgs
                        {
                            GroupId = groupId,
                            FromUserId = senderId,
                            SenderName = senderName,
                            Message = message,
                            Timestamp = timestamp
                        });

                        // لو الجروب ده هو المفتوح حالياً
                        if (groupId == SelectedGroupId
                            && senderId != App.CurrentUser.Id)
                        {
                            using var ctx = new AppDbContext(App.ConnectionString);
                            var sender = await ctx.Users.FindAsync(senderId);

                            Messages.Add(new ChatMessage
                            {
                                MessageText = message,
                                IsFromMe = false,
                                SenderName = sender?.FullName ?? "",
                                Time = timestamp.ToString("hh:mm tt"),
                                SentAt = timestamp,
                                IsDelivered = true,
                                IsRead = true
                            });

                            ScrollToBottom();
                            await ResetUnreadCountAsync(groupId);
                        }
                    });
                });

            _signalRInitialized = true;

            Unloaded += (s, e) =>
            {
                App.SignalRConnection?.Remove("ReceiveGroupMessage");
                _signalRInitialized = false;
            };
        }

        // ── Members Management ───────────────────────────────────────────────

        private void ManageMembers_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserIsAdmin) return;
            var win = new GroupMembersWindow(SelectedGroupId);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
            // refresh members count بعد ما تتقفل
            _ = LoadMembersInfoAsync(SelectedGroupId);
        }

        // ── Attachments ──────────────────────────────────────────────────────

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
                    MessageBox.Show($"الملف {info.Name} أكبر من 10MB");
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

        // ── UI Helpers ───────────────────────────────────────────────────────

        private void SendButton_Click(object sender, RoutedEventArgs e) =>
            SendMessage(MessageTextBox.Text);

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
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
    }
}