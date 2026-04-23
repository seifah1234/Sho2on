using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
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
using System.Windows.Media.Imaging;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace HR_Application.Views.Conversations
{
    public partial class CreateGroupWindow : Window, INotifyPropertyChanged
    {
        private readonly AppDbContext _context;
        private readonly int _currentUserId;
        private byte[] _groupImageData;
        private ObservableCollection<UserSearchItem> _searchResults;
        private ObservableCollection<UserSearchItem> _selectedMembers;
        private List<UserSearchItem> users;

        public ObservableCollection<UserSearchItem> SearchResults
        {
            get => _searchResults;
            set
            {
                _searchResults = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<UserSearchItem> SelectedMembers
        {
            get => _selectedMembers;
            set
            {
                _selectedMembers = value;
                OnPropertyChanged();
            }
        }

        public CreateGroupWindow(int currentUserId)
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _currentUserId = currentUserId;
            SearchResults = new ObservableCollection<UserSearchItem>();
            SelectedMembers = new ObservableCollection<UserSearchItem>();
            DataContext = this;
        }

        private async void SearchMemberBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                await SearchUsersAsync(SearchMemberBox.Text);
            }
        }

        private async Task SearchUsersAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                SearchResults = new ObservableCollection<UserSearchItem>(users);
                return;
            }

            try
            {

                    
                SearchResults = new ObservableCollection<UserSearchItem>(users.Where(u => (u.FullName.Contains(searchText) || u.Code.Contains(searchText)) && !SelectedMembers.Any(m => m.UserId == u.UserId)));

            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}");
            }
        }

        private void AddMember_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var user = border?.Tag as UserSearchItem;
            if (user != null && !SelectedMembers.Any(m => m.UserId == user.UserId))
            {
                SelectedMembers.Add(user);
                SearchResults.Remove(user);
            }
        }

        private void RemoveMember_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var user = button?.Tag as UserSearchItem;
            if (user != null)
            {
                SelectedMembers.Remove(user);
            }
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "اختر صورة للمجموعة"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var fileInfo = new FileInfo(dialog.FileName);
                    if (fileInfo.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("حجم الصورة كبير جداً. الحد الأقصى 5MB");
                        return;
                    }

                    _groupImageData = File.ReadAllBytes(dialog.FileName);
                    LoadImagePreview(_groupImageData);
                    RemoveImageButton.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تحميل الصورة: {ex.Message}");
                }
            }
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            _groupImageData = null;
            GroupImage.Source = new BitmapImage(new Uri("/assets/images/group_avatar.jpg", UriKind.Relative));
            RemoveImageButton.Visibility = Visibility.Collapsed;
        }

        private void LoadImagePreview(byte[] imageData)
        {
            try
            {
                using (var stream = new MemoryStream(imageData))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    GroupImage.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في عرض الصورة: {ex.Message}");
            }
        }

        private async void CreateGroup_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GroupNameBox.Text))
            {
                MessageBox.Show("يرجى إدخال اسم للمجموعة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                GroupNameBox.Focus();
                return;
            }

            if (SelectedMembers.Count == 0)
            {
                MessageBox.Show("يرجى إضافة عضو واحد على الأقل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // إنشاء المجموعة
                var group = new ChatGroup
                {
                    Name = GroupNameBox.Text.Trim(),
                    GroupImageData = _groupImageData,
                    CreatedByUserId = _currentUserId,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.ChatGroups.Add(group);
                await _context.SaveChangesAsync();

                // إضافة الأعضاء
                var members = SelectedMembers.Select(m => new ChatGroupMember
                {
                    GroupId = group.Id,
                    UserId = m.UserId,
                    JoinedAt = DateTime.Now,
                    IsAdmin = false,
                    UnreadCount = 0
                }).ToList();

                // إضافة المنشئ كـ Admin
                members.Add(new ChatGroupMember
                {
                    GroupId = group.Id,
                    UserId = _currentUserId,
                    JoinedAt = DateTime.Now,
                    IsAdmin = true,
                    UnreadCount = 0
                });

                _context.ChatGroupMembers.AddRange(members);
                await _context.SaveChangesAsync();

                // رسالة ترحيب أولية
                var welcomeMessage = new ChatGroupMessage
                {
                    GroupId = group.Id,
                    SenderId = _currentUserId,
                    Message = $"تم إنشاء المجموعة \"{group.Name}\"",
                    SentAt = DateTime.Now
                };
                _context.ChatGroupMessages.Add(welcomeMessage);
                await _context.SaveChangesAsync();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء المجموعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                users = await _context.Users.Where(u => u.Id != App.CurrentUser.Id).Select(u => new UserSearchItem
                {
                    Code = u.Code,
                    FullName = u.FullName,
                    UserId = u.Id,
                    ProfileImageData = u.ProfileImageData ?? new byte[0]
                }).ToListAsync();
                SearchResults = new ObservableCollection<UserSearchItem>(users);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}");
            }
        }
    }

    public class UserSearchItem : INotifyPropertyChanged
    {
        private int _userId;
        private string _fullName;
        private string _code;
        private byte[] _profileImageData;

        public int UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        public string Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(); }
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