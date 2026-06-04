using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for UserPermission.xaml
    /// </summary>
    public partial class UserPermission : Window
    {
        private readonly AppDbContext _context = new AppDbContext(App.ConnectionString);
        private ObservableCollection<UserViewModel> _usersList = new ObservableCollection<UserViewModel>();
        private ObservableCollection<Role> _roles = new ObservableCollection<Role>();

        public UserPermission()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await LoadRolesAsync();
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                roleComboBox.Items.Clear();
                _roles.Clear();

                //  Õ„Ì· «·√œÊ«— „‰ ﬁ«⁄œ… «·»Ì«‰« 
                var roles = await _context.Roles
                    .OrderBy(r => r.RoleName)
                    .ToListAsync();

                // ≈÷«›… ⁄‰’— "·« ‘Ì¡"
                _roles.Add(new Role { RoleID = 0, RoleName = "·« ‘Ì¡" });

                // ≈÷«›… «·√œÊ«— «·√Œ—Ï
                foreach (var role in roles)
                {
                    _roles.Add(role);
                }

                //  ⁄ÌÌ‰ „’œ— «·»Ì«‰«  ··ﬂÊ„»Ê»Êﬂ”
                roleComboBox.ItemsSource = _roles;
                roleComboBox.DisplayMemberPath = "RoleName";
                roleComboBox.SelectedValuePath = "RoleID";
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·√œÊ«—: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                _usersList.Clear();

                //  Õ„Ì· «·„” Œœ„Ì‰ «·–Ì‰ ·œÌÂ„ ’·«ÕÌ… IsUser = true
                var users = await _context.Users
                    .Where(u => u.IsUser)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                foreach (var user in users)
                {
                    var userVM = new UserViewModel
                    {
                        Id = user.Id,
                        Name = user.FullName,
                        Username = user.Username,
                        Code = user.Id,
                        // «·Õ’Ê· ⁄·Ï «·œÊ— «·√Ê· ≈–« ﬂ«‰ „ÊÃÊœ«
                        RoleID = user.UserRoles.FirstOrDefault()?.RoleId ?? 0,
                        Password = user.PasswordHash
                    };

                    _usersList.Add(userVM);
                }

                dataTable.ItemsSource = _usersList;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·„” Œœ„Ì‰: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class UserViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Username { get; set; }
            public int Code { get; set; }
            public int RoleID { get; set; }
            public string Password { get; set; }
        }

        private async void save_btn_Click(object sender, RoutedEventArgs e)
        {
            await SaveDataAsync();
        }

        private async void dataTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataTable.SelectedItem is UserViewModel selectedUser)
            {
                //  ⁄ÌÌ‰ «·œÊ— «·„Õœœ
                roleComboBox.SelectedValue = selectedUser.RoleID;

                //  ⁄ÌÌ‰ ﬂ·„… «·„—Ê— (Ì„ﬂ‰ ≈ŸÂ«—Â« »‘ﬂ· „‘›— ≈–« ·“„ «·√„—)
                pass_box.Password = selectedUser.Password;
                username_box.Text = selectedUser.Username;
            }
        }

        private async Task SaveDataAsync()
        {
            try
            {
                if (dataTable.SelectedItem is not UserViewModel selectedUser)
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— „” Œœ„ Ê«Õœ", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (roleComboBox.SelectedItem is not Role selectedRole)
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— œÊ—", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // «·Õ’Ê· ⁄·Ï «·„” Œœ„ „‰ ﬁ«⁄œ… «·»Ì«‰« 
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == selectedUser.Id);

                if (user == null)
                {
                    LocalizationManager.ShowMessage("«·„” Œœ„ €Ì— „ÊÃÊœ", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //  ÕœÌÀ ﬂ·„… «·„—Ê— ≈–«  „ ≈œŒ«·Â«
                if (!string.IsNullOrEmpty(pass_box.Password))
                {
                    var passwordHash = pass_box.Password;

                    user.PasswordHash = passwordHash;
                }

                //  ÕœÌÀ ﬂ·„… «·„—Ê— ≈–«  „ ≈œŒ«·Â«
                if (!string.IsNullOrEmpty(username_box.Text))
                {
                    user.Username = username_box.Text;
                }

                // ≈œ«—… √œÊ«— «·„” Œœ„
                await ManageUserRolesAsync(user, selectedRole.RoleID);

                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage(" „  ÕœÌÀ ’·«ÕÌ«  «·„” Œœ„ »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);

                // ≈⁄«œ…  Õ„Ì· «·»Ì«‰«  · ÕœÌÀ «·Ê«ÃÂ…
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÕœÀ Œÿ√ √À‰«¡ «·Õ›Ÿ: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ManageUserRolesAsync(User user, int roleId)
        {
            // ≈“«·… Ã„Ì⁄ «·√œÊ«— «·Õ«·Ì… ··„” Œœ„
            var currentUserRoles = user.UserRoles.ToList();
            _context.UserRoles.RemoveRange(currentUserRoles);

            // ≈÷«›… «·œÊ— «·ÃœÌœ ≈–« ·„ Ìﬂ‰ "·« ‘Ì¡"
            if (roleId > 0)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId
                };
                await _context.UserRoles.AddAsync(userRole);
            }
        }

        // œ«·… „”«⁄œ… · ‘›Ì— ﬂ·„… «·„—Ê— (Ì„ﬂ‰ ≈÷«› Â« ≈–« ·“„ «·√„—)
        private string HashPassword(string password)
        {
            // Ì„ﬂ‰ «” Œœ«„ „ﬂ »… „À· BCrypt.Net Â‰«
            return password; // Â–« „À«· »”Ìÿ° ›Ì «· ÿ»Ìﬁ «·ÕﬁÌﬁÌ ÌÃ»  ‘›Ì— ﬂ·„… «·„—Ê—
        }
    }
}
