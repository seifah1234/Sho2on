using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Collections.ObjectModel;
using HR_Application.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows; using HR_Application.Helpers;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views.Conversations
{
    public partial class GroupMembersWindow : Window, INotifyPropertyChanged
    {
        private readonly int _groupId;
        private readonly AppDbContext _ctx;

        private bool _isCurrentUserAdmin;
        private bool _hasSearchResults;

        public bool IsCurrentUserAdmin
        {
            get => _isCurrentUserAdmin;
            set
            {
                _isCurrentUserAdmin = value;
                OnPropertyChanged();
            }
        }

        public bool HasSearchResults
        {
            get => _hasSearchResults;
            set
            {
                _hasSearchResults = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<GroupMemberItem> Members { get; set; } = new();
        public ObservableCollection<UserSearchResult> SearchResults { get; set; } = new();

        public event PropertyChangedEventHandler PropertyChanged;

        public GroupMembersWindow(int groupId)
        {
            InitializeComponent();
            _groupId = groupId;
            _ctx = new AppDbContext(App.ConnectionString);
            DataContext = this;
            Loaded += async (s, e) => await LoadMembersAsync();
        }

        private async Task LoadMembersAsync()
        {
            try
            {
                var members = await _ctx.ChatGroupMembers
                    .Include(m => m.User)
                    .Where(m => m.GroupId == _groupId)
                    .ToListAsync();

                Members.Clear();
                foreach (var m in members)
                {
                    var isAdmin = m.IsAdmin;
                    Members.Add(new GroupMemberItem
                    {
                        UserId = m.UserId,
                        UserName = m.User?.FullName ?? "",
                        UserCode = m.User?.Code ?? "",
                        IsAdmin = isAdmin,
                        ProfileImageData = m.User?.ProfileImageData,
                        CanRemove = !isAdmin || m.UserId != App.CurrentUser.Id,
                        CanManage = m.UserId != App.CurrentUser.Id
                    });

                    // ÊÍÏíÏ ÅÐÇ ßÇä ÇáãÓÊÎÏã ÇáÍÇáí ãÔÑÝÇð
                    if (m.UserId == App.CurrentUser.Id && m.IsAdmin)
                    {
                        IsCurrentUserAdmin = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÎØÃ Ýí ÊÍãíá ÇáÃÚÖÇÁ: {ex.Message}");
            }
        }

        private async void RemoveMember_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as GroupMemberItem;
            if (item == null) return;

            var confirm = LocalizationManager.ShowMessage(
                $"åá ÊÑíÏ ÅÒÇáÉ {item.UserName}¿",
                "ÊÃßíÏ", MessageBoxButton.YesNo);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var member = await _ctx.ChatGroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == _groupId
                                           && m.UserId == item.UserId);
                if (member != null)
                {
                    _ctx.ChatGroupMembers.Remove(member);
                    await _ctx.SaveChangesAsync();
                    Members.Remove(item);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÎØÃ: {ex.Message}");
            }
        }

        private async void ToggleAdmin_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as GroupMemberItem;
            if (item == null) return;

            try
            {
                var member = await _ctx.ChatGroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == _groupId
                                           && m.UserId == item.UserId);
                if (member != null)
                {
                    member.IsAdmin = !member.IsAdmin;
                    await _ctx.SaveChangesAsync();
                    item.IsAdmin = member.IsAdmin;
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÎØÃ: {ex.Message}");
            }
        }

        private async void SearchUser_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;
            var text = (sender as System.Windows.Controls.TextBox)?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                SearchResults.Clear();
                HasSearchResults = false;
                return;
            }

            var users = await _ctx.Users
                .Where(u => u.FullName.Contains(text) || u.Code == text)
                .Where(u => u.Id != App.CurrentUser.Id)
                .Take(10).ToListAsync();

            SearchResults.Clear();
            foreach (var u in users)
            {
                SearchResults.Add(new UserSearchResult
                {
                    UserId = u.Id,
                    UserName = u.FullName,
                    UserCode = u.Code,
                    ProfileImageData = u.ProfileImageData
                });
            }
            HasSearchResults = SearchResults.Count > 0;
        }

        private async void AddMember_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as UserSearchResult;
            if (item == null) return;

            var exists = await _ctx.ChatGroupMembers
                .AnyAsync(m => m.GroupId == _groupId && m.UserId == item.UserId);
            if (exists)
            {
                LocalizationManager.ShowMessage("ÇáãÓÊÎÏã ãæÌæÏ ÈÇáÝÚá Ýí ÇáÌÑæÈ.");
                return;
            }

            _ctx.ChatGroupMembers.Add(new ChatGroupMember
            {
                GroupId = _groupId,
                UserId = item.UserId,
                IsAdmin = false,
                JoinedAt = DateTime.Now
            });
            await _ctx.SaveChangesAsync();
            await LoadMembersAsync();
        }

        protected override void OnClosed(EventArgs e)
        {
            _ctx.Dispose();
            base.OnClosed(e);
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class GroupMemberItem : INotifyPropertyChanged
    {
        private bool _isAdmin;

        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserCode { get; set; }
        public byte[] ProfileImageData { get; set; }
        public bool CanRemove { get; set; }
        public bool CanManage { get; set; }

        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                _isAdmin = value; OnPropertyChanged();
                OnPropertyChanged(nameof(AdminLabel));
            }
        }

        public string AdminLabel => IsAdmin ? "Admin ?" : "ÚÖæ";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
