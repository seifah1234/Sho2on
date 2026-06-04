using HR_Application.Services;
using MahApps.Metro.IconPacks;
using MaterialDesignThemes.Wpf;
using Microsoft.AspNetCore.SignalR.Client;
using HR_Application.Helpers;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
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
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace HR_Application.UserControls
{
    public partial class ChatBox : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty SelectedUserNameProperty =
            DependencyProperty.Register(nameof(SelectedUserName), typeof(string), typeof(ChatBox),
                new PropertyMetadata(""));

        public static readonly DependencyProperty SelectedUserImageProperty =
            DependencyProperty.Register(nameof(SelectedUserImage), typeof(BitmapImage), typeof(ChatBox),
                new PropertyMetadata(null));

        public static readonly DependencyProperty UserStatusProperty =
            DependencyProperty.Register(nameof(UserStatus), typeof(string), typeof(ChatBox),
                new PropertyMetadata("„ ’· «·¬‰"));

        public static readonly DependencyProperty SelectedUserCodeProperty =
            DependencyProperty.Register(nameof(SelectedUserCode), typeof(string), typeof(ChatBox),
                new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedUserIdProperty =
            DependencyProperty.Register(nameof(SelectedUserId), typeof(int), typeof(ChatBox),
                new PropertyMetadata(0));

        public string SelectedUserName
        {
            get => (string)GetValue(SelectedUserNameProperty);
            set => SetValue(SelectedUserNameProperty, value);
        }

        public int SelectedUserId
        {
            get => (int)GetValue(SelectedUserIdProperty);
            set => SetValue(SelectedUserIdProperty, value);
        }

        public BitmapImage SelectedUserImage
        {
            get => (BitmapImage)GetValue(SelectedUserImageProperty);
            set => SetValue(SelectedUserImageProperty, value);
        }

        public string UserStatus
        {
            get => (string)GetValue(UserStatusProperty);
            set => SetValue(UserStatusProperty, value);
        }

        public string SelectedUserCode
        {
            get => (string)GetValue(SelectedUserCodeProperty);
            set => SetValue(SelectedUserCodeProperty, value);
        }

        public ObservableCollection<UIChatMessage> Messages { get; set; }
        private bool _signalRInitialized = false;
        public event EventHandler<NewMessageEventArgs> NewMessageReceived;
        public event EventHandler<NewMessageEventArgs> NewMessageSent;
        public event EventHandler<MessageUpdatedEventArgs> MessageUpdated;

        public ObservableCollection<ChatAttachmentItem> SelectedAttachments { get; set; }
        private List<ChatAttachmentItem> _pendingAttachments = new List<ChatAttachmentItem>();
        private int _editingMessageId = -1;
        public ChatBox()
        {
            InitializeComponent();
            Messages = new ObservableCollection<UIChatMessage>();
            SelectedAttachments = new ObservableCollection<ChatAttachmentItem>();
            MessagesItemsControl.ItemsSource = Messages;
            DataContext = this;

            // «” œ⁄«¡ ≈⁄œ«œ SignalR
            Loaded += (s, e) => SetupSignalRListener();
            LoadSavedBackground();
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            _editingMessageId = -1;
            MessageTextBox.Text = "";
            EditBar.Visibility = Visibility.Collapsed;
        }

        private void EditMessage_Click(object sender, RoutedEventArgs e)
        {
            var msg = GetMessageFromContextMenu(sender);
            if (msg == null || !msg.IsFromMe) return;

            _editingMessageId = msg.MessageDbId;
            MessageTextBox.Text = msg.MessageText;
            MessageTextBox.Focus();
            MessageTextBox.CaretIndex = MessageTextBox.Text.Length;

            EditBar.Visibility = Visibility.Visible;
            EditingLabel.Text = $"??  ⁄œÌ·: {msg.MessageText[..Math.Min(30, msg.MessageText.Length)]}...";
        }

        private async void DeleteMessage_Click(object sender, RoutedEventArgs e)
        {
            var msg = GetMessageFromContextMenu(sender);
            if (msg == null || !msg.IsFromMe) return;

            var confirm = LocalizationManager.ShowMessage("Â·  —Ìœ Õ–› Â–Â «·—”«·…ø", " √ﬂÌœ",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new AppDbContext(App.ConnectionString);
                var dbMsg = await ctx.ChatMessages.FindAsync(msg.MessageDbId);
                if (dbMsg != null)
                {
                    dbMsg.IsDeleted = true;
                    await ctx.SaveChangesAsync();
                }

                Messages.Remove(msg);

                // ≈»·€ «·ÿ—› «·¬Œ— ⁄»— SignalR
                if (App.SignalRConnection?.State == HubConnectionState.Connected)
                {
                    await App.SignalRConnection.InvokeAsync(
                        "MessageDeleted", msg.MessageDbId, SelectedUserId);
                }

                // FIX BUG #5: Notify parent about message update (for last message)
                MessageUpdated?.Invoke(this, new MessageUpdatedEventArgs
                {
                    OtherUserId = SelectedUserId,
                    LastMessage = GetLastMessageText(),
                    LastMessageTime = GetLastMessageTime()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteMessage error: {ex.Message}");
            }
        }

        // FIX BUG #5: Helper methods to get last message info
        private string GetLastMessageText()
        {
            var lastMsg = Messages.LastOrDefault();
            if (lastMsg == null) return "·«  ÊÃœ —”«∆·";
            if (string.IsNullOrEmpty(lastMsg.MessageText)) return "?? „—›ﬁ";
            return lastMsg.MessageText;
        }

        private DateTime GetLastMessageTime()
        {
            var lastMsg = Messages.LastOrDefault();
            return lastMsg?.SentAt ?? DateTime.Now;
        }

        private async void AttachButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "All files (*.*)|*.*",
                Title = "«Œ — «·„·›«  ·≈—›«ﬁÂ«"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var filePath in dialog.FileNames)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);

                        // «·Õœ «·√ﬁ’Ï ··ÕÃ„ 10MB
                        if (fileInfo.Length > 10 * 1024 * 1024)
                        {
                            LocalizationManager.ShowMessage($"«·„·› {fileInfo.Name} ÕÃ„Â ﬂ»Ì— Ãœ« (Õœ √ﬁ’Ï 10MB)", " ‰»ÌÂ",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            continue;
                        }

                        var fileData = File.ReadAllBytes(filePath);
                        var attachment = new ChatAttachmentItem
                        {
                            FileName = fileInfo.Name,
                            FileSize = fileInfo.Length,
                            FileData = fileData,
                            FileIcon = GetFileIcon(fileInfo.Extension)
                        };

                        _pendingAttachments.Add(attachment);
                        SelectedAttachments.Add(attachment);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error adding attachment: {ex.Message}");
                    }
                }

                // ≈ŸÂ«—/≈Œ›«¡ „‰ÿﬁ… «·„—›ﬁ« 
                AttachmentsScrollViewer.Visibility = SelectedAttachments.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
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
                        LocalizationManager.ShowMessage(" „ Õ›Ÿ «·„·› »‰Ã«Õ", " „", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        LocalizationManager.ShowMessage($"Œÿ√ ›Ì Õ›Ÿ «·„·›: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // ≈“«·… „—›ﬁ
        private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var attachment = button?.Tag as ChatAttachmentItem;

            if (attachment != null)
            {
                _pendingAttachments.Remove(attachment);
                SelectedAttachments.Remove(attachment);
                AttachmentsScrollViewer.Visibility = SelectedAttachments.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        //  ÕœÌœ √ÌﬁÊ‰… «·„·› Õ”» «·«„ œ«œ
        private string GetFileIcon(string extension)
        {
            return extension.ToLower() switch
            {
                ".pdf" => "FilePdf",
                ".doc" or ".docx" => "FileWord",
                ".xls" or ".xlsx" => "FileExcel",
                ".ppt" or ".pptx" => "FilePowerpoint",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "FileImage",
                ".mp4" or ".avi" or ".mkv" => "FileVideo",
                ".mp3" or ".wav" => "FileMusic",
                ".zip" or ".rar" or ".7z" => "FileZip",
                _ => "File"
            };
        }

        public async void LoadChat(string userName, string userCode, byte[] userImageData = null, int userId = 0)
        {
            SelectedUserName = userName;
            SelectedUserCode = userCode;
            SelectedUserId = userId;

            //  ÕÊÌ· byte[] ≈·Ï BitmapImage »‘ﬂ· ’ÕÌÕ
            if (userImageData != null && userImageData.Length > 0)
            {
                SelectedUserImage = ConvertByteArrayToBitmapImage(userImageData);
            }
            else
            {
                SelectedUserImage = new BitmapImage(new Uri("/assets/images/avatar.jpg", UriKind.Relative));
            }

            // ≈Œ›«¡ «·‹ Placeholder
            NoChatSelectedPlaceholder.Visibility = Visibility.Collapsed;

            //  ›⁄Ì· „‰ÿﬁ… «·‘« 
            MessageTextBox.IsEnabled = true;
            SendButton.IsEnabled = true;

            // „”Õ «·—”«∆· «·”«»ﬁ…
            Messages.Clear();

            //  Õ„Ì· «·—”«∆· „‰ ﬁ«⁄œ… «·»Ì«‰« 
            await LoadMessagesFromDatabase(userId);


            await UpdateMessageDeliveredStatus(userId);

            await SendDeliveredNotification(userId);

            await MarkMessageAsRead(userId);
        }

        public void ClearChat()
        {
            SelectedUserName = "";
            SelectedUserCode = "";
            SelectedUserImage = null;
            SelectedUserId = 0;
            // ≈ŸÂ«— «·‹ Placeholder
            NoChatSelectedPlaceholder.Visibility = Visibility.Visible;
            //  ⁄ÿÌ· „‰ÿﬁ… «·‘« 
            MessageTextBox.IsEnabled = false;
            SendButton.IsEnabled = false;
            // „”Õ «·—”«∆·
            Messages.Clear();
        }

        // œ«·… „”«⁄œ… · ÕÊÌ· byte[] ≈·Ï BitmapImage
        private BitmapImage ConvertByteArrayToBitmapImage(byte[] imageData)
        {
            try
            {
                using (var stream = new System.IO.MemoryStream(imageData))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze(); // „Â„ ·· Ã„Ìœ
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting image: {ex.Message}");
                return new BitmapImage(new Uri("/assets/images/avatar.jpg", UriKind.Relative));
            }
        }

        private async Task LoadMessagesFromDatabase(int userId)
        {
            try
            {
                if (App.CurrentUser == null)
                {
                    Console.WriteLine("CurrentUser is null");
                    return;
                }

                using (var context = new AppDbContext(App.ConnectionString))
                {
                    // «·»ÕÀ ⁄‰ «·„Õ«œÀ…
                    var chat = await context.Chats
                        .FirstOrDefaultAsync(c =>
                            (c.FirstUserId == App.CurrentUser.Id && c.SecondUserId == userId) ||
                            (c.FirstUserId == userId && c.SecondUserId == App.CurrentUser.Id));

                    if (chat != null)
                    {
                        // Ã·» «·—”«∆·
                        var messages = await context.ChatMessages
                            .Where(m => m.ChatId == chat.Id && !m.IsDeleted)
                            .OrderBy(m => m.SentAt)
                            .ToListAsync();

                        Messages.Clear();
                        foreach (var msg in messages)
                        {
                            var uiMessage = new UIChatMessage
                            {
                                MessageDbId = msg.Id,
                                MessageText = msg.Message,
                                IsFromMe = msg.SenderId == App.CurrentUser.Id,
                                Time = msg.SentAt.ToString("hh:mm tt"),
                                SentAt = msg.SentAt,
                                IsRead = msg.IsRead,
                                IsDelivered = msg.IsDelivered ?? false,
                                IsEdited = msg.IsEdited
                            };

                            await LoadAttachmentsForMessage(msg.Id, uiMessage);

                            Messages.Add(uiMessage);
                        }

                        //  ÕœÌÀ Õ«·… «·ﬁ—«¡… ··—”«∆· €Ì— «·„ﬁ—Ê¡…
                        var unreadMessages = messages.Where(m => m.ReceiverId == App.CurrentUser.Id && !m.IsRead).ToList();
                        if (unreadMessages.Any())
                        {
                            foreach (var msg in unreadMessages)
                            {
                                msg.IsRead = true;
                                msg.ReadAt = DateTime.Now;
                            }
                            await context.SaveChangesAsync();
                        }

                        //  ÕœÌÀ Õ«·… «· ”·Ì„ ··—”«∆· «· Ì Ê’·  (€Ì— „”·„…)
                        var undeliveredMessages = messages.Where(m => m.ReceiverId == App.CurrentUser.Id && m.IsDelivered.HasValue && !m.IsDelivered.Value).ToList();
                        if (undeliveredMessages.Any())
                        {
                            foreach (var msg in undeliveredMessages)
                            {
                                msg.IsDelivered = true;
                            }
                            await context.SaveChangesAsync();

                            //  ÕœÌÀ «·Ê«ÃÂ…
                            foreach (var msg in Messages.Where(m => !m.IsFromMe && !m.IsDelivered))
                            {
                                msg.IsDelivered = true;
                            }
                        }

                        ScrollToBottom();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading messages: {ex.Message}");
                LogError($"Error loading messages: {ex.Message}");
            }
        }
        private async void SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) && _pendingAttachments.Count == 0)
                return;

            if (_editingMessageId != -1)
            {
                await SaveEditAsync(_editingMessageId, message);
                return;
            }

            // ≈÷«›… «·—”«·… ·ﬁ«∆„… «·—”«∆· „ƒﬁ «
            var tempMessage = new UIChatMessage
            {
                MessageText = message ?? "",
                IsFromMe = true,
                Time = DateTime.Now.ToString("hh:mm tt"),
                SentAt = DateTime.Now,
                IsDelivered = false,
                IsRead = false
            };

            foreach (var attachment in _pendingAttachments)
            {
                tempMessage.Attachments.Add(new ChatAttachmentItem
                {
                    FileName = attachment.FileName,
                    FileSize = attachment.FileSize,
                    FileData = attachment.FileData,
                    FileIcon = attachment.FileIcon
                });
            }

            Messages.Add(tempMessage);

            // „”Õ Õﬁ· «·ﬂ «»…
            MessageTextBox.Text = "";
            var attachmentsToSend = _pendingAttachments.ToList();
            _pendingAttachments.Clear();
            SelectedAttachments.Clear();
            AttachmentsScrollViewer.Visibility = Visibility.Collapsed;

            // «· „—Ì— ··√”›·
            ScrollToBottom();

            // Õ›Ÿ Ê≈—”«· «·—”«·…
            await SendToServer(message ?? "", attachmentsToSend);
        }

        private async Task SaveEditAsync(int messageDbId, string newText)
        {
            try
            {
                using var ctx = new AppDbContext(App.ConnectionString);
                var dbMsg = await ctx.ChatMessages.FindAsync(messageDbId);

                if (dbMsg == null) return;

                dbMsg.Message = newText;
                dbMsg.IsEdited = true;
                dbMsg.EditedAt = DateTime.Now;
                await ctx.SaveChangesAsync();

                // Update UI immediately for sender
                var uiMsg = Messages.FirstOrDefault(m => m.MessageDbId == messageDbId);
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
                }

                // ≈»·€ «·ÿ—› «·¬Œ—
                if (App.SignalRConnection?.State == HubConnectionState.Connected)
                {
                    await App.SignalRConnection.InvokeAsync(
                        "MessageEdited", messageDbId, SelectedUserId, newText);
                }

                // ? FIX: Check if this is the last message and notify parent
                var lastMsg = Messages.LastOrDefault();
                if (lastMsg != null && lastMsg.MessageDbId == messageDbId)
                {
                    MessageUpdated?.Invoke(this, new MessageUpdatedEventArgs
                    {
                        OtherUserId = SelectedUserId,
                        LastMessage = GetLastMessageText(),
                        LastMessageTime = GetLastMessageTime()
                    });
                }

                // reset
                _editingMessageId = -1;
                MessageTextBox.Text = "";
                EditBar.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EditMessage error: {ex.Message}");
            }
        }

        // helper
        private UIChatMessage GetMessageFromContextMenu(object sender)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Tag is UIChatMessage msg) return msg;
                if (mi.Parent is ContextMenu cm &&
                    cm.PlacementTarget is Border border)
                    return border.Tag as UIChatMessage;
            }
            return null;
        }
        private async Task SendToServer(string message, List<ChatAttachmentItem> attachments)
        {
            try
            {
                if (App.CurrentUser == null)
                {
                    LogError("CurrentUser is null");
                    return;
                }

                using (var context = new AppDbContext(App.ConnectionString))
                {
                    // «·»ÕÀ ⁄‰ «·„Õ«œÀ… »Ì‰ «·„” Œœ„Ì‰
                    var chat = await context.Chats
                        .FirstOrDefaultAsync(c =>
                            (c.FirstUserId == App.CurrentUser.Id && c.SecondUserId == SelectedUserId) ||
                            (c.FirstUserId == SelectedUserId && c.SecondUserId == App.CurrentUser.Id));

                    if (chat == null)
                    {
                        // ≈‰‘«¡ „Õ«œÀ… ÃœÌœ… ≈–« ·„  ﬂ‰ „ÊÃÊœ…
                        chat = new Chat
                        {
                            FirstUserId = App.CurrentUser.Id,
                            SecondUserId = SelectedUserId,
                            CreatedAt = DateTime.Now,
                            IsActive = true
                        };
                        context.Chats.Add(chat);
                        await context.SaveChangesAsync();
                    }

                    // ≈‰‘«¡ —”«·… ÃœÌœ…
                    var chatMessage = new Sho2on.Database.Models.ChatMessage
                    {
                        ChatId = chat.Id,
                        SenderId = App.CurrentUser.Id,
                        ReceiverId = SelectedUserId,
                        Message = message,
                        SentAt = DateTime.Now,
                        IsRead = false,
                        IsDeleted = false,
                        IsDelivered = false
                    };

                    context.ChatMessages.Add(chatMessage);
                    await context.SaveChangesAsync();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // ¬Œ— —”«·… √÷›‰«Â« ÂÌ «·‹ tempMessage
                        var lastMsg = Messages.LastOrDefault(m => m.IsFromMe && m.MessageDbId == 0);
                        if (lastMsg != null)
                            lastMsg.MessageDbId = chatMessage.Id;
                    });

                    // Õ›Ÿ «·„—›ﬁ« 
                    foreach (var attachment in attachments)
                    {
                        var dbAttachment = new Sho2on.Database.Models.ChatAttachment
                        {
                            MessageId = chatMessage.Id,
                            FileName = attachment.FileName,
                            FileSize = attachment.FileSize,
                            FileData = attachment.FileData,
                            ContentType = GetContentType(attachment.FileName),
                            CreatedAt = DateTime.Now
                        };
                        context.ChatAttachments.Add(dbAttachment);
                    }

                    await context.SaveChangesAsync();

                    // „”Õ «·„—›ﬁ«  «·„⁄·ﬁ…
                    _pendingAttachments.Clear();
                    SelectedAttachments.Clear();
                    AttachmentsScrollViewer.Visibility = Visibility.Collapsed;

                    //  ÕœÌÀ Êﬁ  ¬Œ— —”«·… ›Ì «·„Õ«œÀ…
                    chat.UpdatedAt = DateTime.Now;
                    await context.SaveChangesAsync();

                    // ≈—”«· ≈‘⁄«— ›Ê—Ì ··„” Œœ„ «·¬Œ— ⁄»— SignalR
                    await SendRealTimeNotification(SelectedUserId, message);

                    await UpdateMessageDeliveredStatus(SelectedUserId);

                    NewMessageSent?.Invoke(this, new NewMessageEventArgs
                    {
                        FromUserId = App.CurrentUser.Id,
                        ToUserId = SelectedUserId,
                        Message = message,
                        Timestamp = DateTime.Now,
                        IsFromMe = true
                    });
                }
            }
            catch (Exception ex)
            {
                LogError($"Œÿ√ ›Ì ≈—”«· «·—”«·…: {ex.Message}");

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LocalizationManager.ShowMessage("›‘· ›Ì ≈—”«· «·—”«·…. Ì—ÃÏ «·„Õ«Ê·… „—… √Œ—Ï.", "Œÿ√",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }

        //  Õ„Ì· «·„—›ﬁ«  „⁄ «·—”«∆·
        private async Task LoadAttachmentsForMessage(int messageId, UIChatMessage uiMessage)
        {
            try
            {
                using (var context = new AppDbContext(App.ConnectionString))
                {
                    var attachments = await context.ChatAttachments
                        .Where(a => a.MessageId == messageId)
                        .ToListAsync();

                    if (attachments.Any())
                    {
                        foreach (var att in attachments)
                        {
                            uiMessage.Attachments.Add(new ChatAttachmentItem
                            {
                                FileName = att.FileName,
                                FileSize = att.FileSize,
                                FileData = att.FileData,
                                FileIcon = GetFileIcon(Path.GetExtension(att.FileName))
                            });
                        }
                        Console.WriteLine($"Loaded {attachments.Count} attachments for message {messageId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading attachments: {ex.Message}");
            }
        }

        private async Task SendRealTimeNotification(int receiverId, string message)
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

                        // ≈⁄«œ…  ”ÃÌ· «·„” Œœ„ »⁄œ ≈⁄«œ… «·« ’«·
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


                // ≈—”«· «·—”«·…
                await App.SignalRConnection.InvokeAsync("SendMessageToUser",
                    App.CurrentUser.Id, receiverId, message);

            }
            catch (Exception ex)
            {
                LogError($"SignalR error: {ex.Message}");
            }
        }

        // ›Ì SetupSignalRListener ó «” »œ· «·ﬂÊœ «·ﬁœÌ„ »œÂ:

        private void SetupSignalRListener()
        {
            if (_signalRInitialized) return;

            var manager = SignalRManager.Instance;

            // Use local methods to ensure proper event handling
            manager.OnMessageReceived += HandleIncomingMessage;
            manager.OnMessageDelivered += HandleMessageDelivered;
            manager.OnMessageRead += HandleMessageRead;
            manager.OnMessageDeleted += HandleMessageDeleted;
            manager.OnMessageEdited += HandleMessageEdited;

            _signalRInitialized = true;

            // Ensure cleanup on unload
            Unloaded += (s, e) =>
            {
                manager.OnMessageReceived -= HandleIncomingMessage;
                manager.OnMessageDelivered -= HandleMessageDelivered;
                manager.OnMessageRead -= HandleMessageRead;
                manager.OnMessageDeleted -= HandleMessageDeleted;
                manager.OnMessageEdited -= HandleMessageEdited;
                _signalRInitialized = false;
            };
        }

        // FIX BUG #6: Handle message deletion in real-time
        private void HandleMessageDeleted(int messageId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var msg = Messages.FirstOrDefault(m => m.MessageDbId == messageId);
                if (msg != null)
                {
                    Messages.Remove(msg);

                    // Notify parent about the change for last message update
                    MessageUpdated?.Invoke(this, new MessageUpdatedEventArgs
                    {
                        OtherUserId = SelectedUserId,
                        LastMessage = GetLastMessageText(),
                        LastMessageTime = GetLastMessageTime()
                    });
                }
            });
        }

        // FIX BUG #1: Handle message editing in real-time
        private void HandleMessageEdited(int messageId, string newText)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var msg = Messages.FirstOrDefault(m => m.MessageDbId == messageId);
                if (msg != null)
                {
                    msg.MessageText = newText;
                    msg.IsEdited = true;

                    // Force UI refresh by replacing the item
                    var index = Messages.IndexOf(msg);
                    if (index >= 0)
                    {
                        Messages.RemoveAt(index);
                        Messages.Insert(index, msg);
                    }

                    // ? FIX: Check if this is the last message and notify parent
                    var lastMsg = Messages.LastOrDefault();
                    if (lastMsg != null && lastMsg.MessageDbId == messageId)
                    {
                        MessageUpdated?.Invoke(this, new MessageUpdatedEventArgs
                        {
                            OtherUserId = SelectedUserId,
                            LastMessage = GetLastMessageText(),
                            LastMessageTime = GetLastMessageTime()
                        });
                    }
                }
            });
        }

        private async void HandleIncomingMessage(
    int fromUserId, int toUserId, string message, DateTime timestamp)
        {
            if (fromUserId != SelectedUserId) return;

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var uiMessage = new UIChatMessage
                {
                    MessageText = message,
                    IsFromMe = false,
                    Time = timestamp.ToString("hh:mm tt"),
                    SentAt = timestamp,
                    IsRead = false,
                    IsDelivered = true
                };

                Messages.Add(uiMessage);

                // ? FIX: Load attachments from database for this message
                await LoadAttachmentsForLatestMessage(fromUserId, uiMessage);

                ScrollToBottom();
            });

            _ = MarkMessageAsRead(fromUserId);
            _ = UpdateMessageDeliveredStatus(fromUserId);

            // Reset unread for this chat
            using var ctx = new AppDbContext(App.ConnectionString);
            var chat = await ctx.Chats.FirstOrDefaultAsync(c =>
                (c.FirstUserId == App.CurrentUser.Id && c.SecondUserId == fromUserId) ||
                (c.FirstUserId == fromUserId && c.SecondUserId == App.CurrentUser.Id));
            if (chat != null)
                await SignalRManager.Instance.ResetUnreadCountAsync(chat.Id, App.CurrentUser.Id);

            NewMessageReceived?.Invoke(this, new NewMessageEventArgs
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Message = message,
                Timestamp = timestamp,
                IsFromMe = false
            });
        }

        private async Task LoadAttachmentsForLatestMessage(int senderId, UIChatMessage uiMessage)
        {
            try
            {
                using var context = new AppDbContext(App.ConnectionString);

                // Find the latest message from this sender in this chat
                var latestMessage = await context.ChatMessages
                    .Where(m => m.SenderId == senderId
                             && m.ReceiverId == App.CurrentUser.Id
                             && !m.IsDeleted)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                if (latestMessage != null)
                {
                    // Set the message ID
                    uiMessage.MessageDbId = latestMessage.Id;

                    // Load attachments
                    var attachments = await context.ChatAttachments
                        .Where(a => a.MessageId == latestMessage.Id)
                        .ToListAsync();

                    foreach (var att in attachments)
                    {
                        uiMessage.Attachments.Add(new ChatAttachmentItem
                        {
                            FileName = att.FileName,
                            FileSize = att.FileSize,
                            FileData = att.FileData,
                            FileIcon = GetFileIcon(Path.GetExtension(att.FileName))
                        });
                    }

                    Console.WriteLine($"Loaded {attachments.Count} attachments for incoming message {latestMessage.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading attachments for incoming message: {ex.Message}");
            }
        }
        private void HandleMessageDelivered(int fromUserId, int toUserId)
        {
            if (toUserId != SelectedUserId && fromUserId != SelectedUserId) return;
            foreach (var msg in Messages.Where(m => m.IsFromMe && !m.IsDelivered))
                msg.IsDelivered = true;
        }

        private void HandleMessageRead(int fromUserId, int toUserId)
        {
            if (toUserId != SelectedUserId && fromUserId != SelectedUserId) return;
            foreach (var msg in Messages.Where(m => m.IsFromMe && !m.IsRead))
                msg.IsRead = true;
        }

        private async Task MarkMessageAsRead(int fromUserId)
        {
            try
            {
                using (var context = new AppDbContext(App.ConnectionString))
                {
                    var chat = await context.Chats
                        .FirstOrDefaultAsync(c =>
                            (c.FirstUserId == App.CurrentUser.Id && c.SecondUserId == fromUserId) ||
                            (c.FirstUserId == fromUserId && c.SecondUserId == App.CurrentUser.Id));

                    if (chat != null)
                    {
                        var unreadMessages = await context.ChatMessages
                            .Where(m => m.ChatId == chat.Id && m.SenderId == fromUserId && !m.IsRead)
                            .ToListAsync();

                        foreach (var msg in unreadMessages)
                        {
                            msg.IsRead = true;
                            msg.ReadAt = DateTime.Now;
                        }

                        await context.SaveChangesAsync();

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var msg in Messages.Where(m => !m.IsFromMe && !m.IsRead))
                            {
                                msg.IsRead = true;
                            }
                        });


                        await SendReadNotification(fromUserId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking as read: {ex.Message}");
            }
        }

        private async Task SendReadNotification(int senderId)
        {
            try
            {
                if (App.SignalRConnection != null && App.SignalRConnection.State == HubConnectionState.Connected)
                {
                    await App.SignalRConnection.InvokeAsync("MessageRead", App.CurrentUser.Id, senderId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending read notification: {ex.Message}");
            }
        }

        private async Task UpdateMessageDeliveredStatus(int toUserId)
        {
            try
            {
                using (var context = new AppDbContext(App.ConnectionString))
                {
                    var chat = await context.Chats
                        .FirstOrDefaultAsync(c =>
                            (c.FirstUserId == App.CurrentUser.Id && c.SecondUserId == toUserId) ||
                            (c.FirstUserId == toUserId && c.SecondUserId == App.CurrentUser.Id));

                    if (chat != null)
                    {
                        //  ÕœÌÀ «·—”«∆· €Ì— «·„”·„… «· Ì √—”·Â« «·„” Œœ„ «·¬Œ— ≈·Ì¯
                        var undeliveredMessages = await context.ChatMessages
                            .Where(m => m.ChatId == chat.Id &&
                                        m.SenderId == toUserId &&
                                        m.ReceiverId == App.CurrentUser.Id &&
                                        m.IsDelivered.HasValue &&
                                        !m.IsDelivered.Value)
                            .ToListAsync();

                        if (undeliveredMessages.Any())
                        {
                            foreach (var msg in undeliveredMessages)
                            {
                                msg.IsDelivered = true;
                            }
                            await context.SaveChangesAsync();

                            //  ÕœÌÀ «·Ê«ÃÂ…
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                foreach (var msg in Messages.Where(m => !m.IsFromMe && !m.IsDelivered))
                                {
                                    msg.IsDelivered = true;
                                }
                            });

                            Console.WriteLine($"Updated {undeliveredMessages.Count} messages as delivered");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating delivered status: {ex.Message}");
            }
        }

        // √÷› Â–Â «·œ«·… ·≈—”«· ≈‘⁄«— »«·Ê’Ê· (Delivered)
        private async Task SendDeliveredNotification(int senderId)
        {
            try
            {
                if (App.SignalRConnection != null && App.SignalRConnection.State == HubConnectionState.Connected)
                {
                    await App.SignalRConnection.InvokeAsync("MessageDelivered", App.CurrentUser.Id, senderId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending delivered notification: {ex.Message}");
            }
        }

        private void LogError(string message)
        {
            try
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "chat_errors.log");
                string logDir = System.IO.Path.GetDirectoryName(logPath);
                if (!System.IO.Directory.Exists(logDir))
                    System.IO.Directory.CreateDirectory(logDir);

                System.IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch { }
        }

        private void ScrollToBottom()
        {
            MessagesScrollViewer?.Dispatcher.Invoke(() =>
            {
                MessagesScrollViewer?.ScrollToEnd();
            });
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(MessageTextBox.Text);
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                e.Handled = true;
                SendMessage(MessageTextBox.Text);
            }
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ChatAttachmentItem : INotifyPropertyChanged
    {
        private string _fileName;
        private long _fileSize;
        private string _fileIcon;
        private byte[] _fileData;
        private bool _isNew = true;

        public bool IsNew
        {
            get => _isNew;
            set { _isNew = value; OnPropertyChanged(); }
        }
        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        public long FileSize
        {
            get => _fileSize;
            set { _fileSize = value; OnPropertyChanged(); }
        }

        public string FileSizeText => FormatFileSize(FileSize);

        public string FileIcon
        {
            get => _fileIcon;
            set { _fileIcon = value; OnPropertyChanged(); }
        }

        public byte[] FileData
        {
            get => _fileData;
            set { _fileData = value; OnPropertyChanged(); }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


    // ‰„Ê–Ã «·—”«·…
    public class UIChatMessage : INotifyPropertyChanged
    {
        private string _messageText;
        private bool _isFromMe;
        private string _time;
        private DateTime _sentAt;
        private bool _isRead;
        private bool _isDelivered;
        private ObservableCollection<ChatAttachmentItem> _attachments;
        private int _readCount;
        private bool _isEdited;
        private int _messageDbId;

        public int ReadCount
        {
            get => _readCount;
            set
            {
                _readCount = value; OnPropertyChanged();
                OnPropertyChanged(nameof(GroupStatusIcon));
            }
        }
        private string _senderName;
        public string SenderName
        {
            get => _senderName;
            set { _senderName = value; OnPropertyChanged(); }
        }
        public ObservableCollection<ChatAttachmentItem> Attachments
        {
            get => _attachments ??= new ObservableCollection<ChatAttachmentItem>();
            set { _attachments = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasAttachments)); OnPropertyChanged(nameof(AttachmentsText)); }
        }

        public string MessageText
        {
            get => _messageText;
            set { _messageText = value; OnPropertyChanged(); }
        }
        public int MessageDbId
        {
            get => _messageDbId;
            set { _messageDbId = value; OnPropertyChanged(); }
        }
        public bool IsEdited
        {
            get => _isEdited;
            set
            {
                _isEdited = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditedLabel));
                OnPropertyChanged(nameof(ShowEditedLabel));
            }
        }

        public bool IsFromMe
        {
            get => _isFromMe;
            set { _isFromMe = value; OnPropertyChanged(); }
        }

        public string Time
        {
            get => _time;
            set { _time = value; OnPropertyChanged(); }
        }

        public DateTime SentAt
        {
            get => _sentAt;
            set { _sentAt = value; OnPropertyChanged(); }
        }

        public bool IsRead
        {
            get => _isRead;
            set { _isRead = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusIcon)); }
        }

        public bool IsDelivered
        {
            get => _isDelivered;
            set { _isDelivered = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusIcon)); }
        }

        public HorizontalAlignment Alignment => IsFromMe ? HorizontalAlignment.Left : HorizontalAlignment.Right;

        public Visibility MyMessageVisibility => IsFromMe ? Visibility.Visible : Visibility.Collapsed;
        public Visibility OtherMessageVisibility => !IsFromMe ? Visibility.Visible : Visibility.Collapsed;

        public string StatusIcon => IsRead ? "Eye" : (IsDelivered ? "CheckAll" : "Check");

        public bool HasAttachments => Attachments?.Count > 0;
        public string AttachmentsText => HasAttachments ? $"?? {Attachments.Count} „·›" : "";

        public string GroupStatusIcon => ReadCount > 0 ? "CheckAll" : "Check";
        public string ReadCountText => ReadCount > 0 ? $"?? {ReadCount}" : "?";
        public string EditedLabel => IsEdited ? "??  „ «· ⁄œÌ·" : "";
        public bool ShowEditedLabel => IsEdited;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


    public class NewMessageEventArgs : EventArgs
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsFromMe { get; set; }
    }

    // FIX BUG #5: New event args for message updates (edit/delete)
    public class MessageUpdatedEventArgs : EventArgs
    {
        public int OtherUserId { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }
    }
}
