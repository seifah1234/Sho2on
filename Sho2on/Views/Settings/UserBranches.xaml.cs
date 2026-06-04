using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    public partial class UserBranches : Window
    {
        private readonly AppDbContext _context = new AppDbContext(App.ConnectionString);
        private ObservableCollection<BranchViewModel> _branches = new ObservableCollection<BranchViewModel>();
        private ObservableCollection<User> _users = new ObservableCollection<User>();

        public UserBranches()
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
                await LoadUsersAsync();
                await LoadBranchesAsync();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                userComboBox.Items.Clear();
                _users.Clear();

                var users = await _context.Users
                    .Where(u => u.IsUser)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                foreach (var user in users)
                {
                    _users.Add(user);
                    userComboBox.Items.Add(user);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·„” Œœ„Ì‰: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadBranchesAsync()
        {
            try
            {
                _branches.Clear();

                var branches = await _context.Branches
                    .OrderBy(b => b.Name)
                    .ToListAsync();

                foreach (var branch in branches)
                {
                    var branchVM = new BranchViewModel
                    {
                        Id = branch.Id,
                        Name = branch.Name,
                        IsActive = false
                    };
                    _branches.Add(branchVM);
                }

                dataTable.ItemsSource = _branches;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·›—Ê⁄: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class BranchViewModel : INotifyPropertyChanged
        {
            private bool _isActive;

            public int Id { get; set; }
            public string Name { get; set; }

            public bool IsActive
            {
                get => _isActive;
                set
                {
                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private async void saveData()
        {
            try
            {
                if (userComboBox.SelectedItem == null)
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— „” Œœ„ √Ê·«", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedUserName = userComboBox.SelectedItem as User;
                if (selectedUserName == null)
                {
                    LocalizationManager.ShowMessage("«·„” Œœ„ «·„Õœœ €Ì— ’«·Õ", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var selectedUser = _users.FirstOrDefault(u => u.Id == selectedUserName.Id);

                if (selectedUser == null)
                {
                    LocalizationManager.ShowMessage("«·„” Œœ„ «·„Õœœ €Ì— „ÊÃÊœ", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // «·Õ’Ê· ⁄·Ï «·›—Ê⁄ «·Õ«·Ì… ··„” Œœ„
                var currentUserBranches = await _context.UserBranches
                    .Where(ub => ub.UserID == selectedUser.Id)
                    .ToListAsync();

                // „⁄«·Ã… «·›—Ê⁄ «·„Õœœ…
                foreach (var branchVM in _branches)
                {
                    var existingUserBranch = currentUserBranches
                        .FirstOrDefault(ub => ub.BranchId == branchVM.Id);

                    if (branchVM.IsActive && existingUserBranch == null)
                    {
                        // ≈÷«›… ›—⁄ ÃœÌœ ··„” Œœ„
                        var userBranch = new UserBranch
                        {
                            UserID = selectedUser.Id,
                            BranchId = branchVM.Id,
                        };
                        await _context.UserBranches.AddAsync(userBranch);
                    }
                    else if (!branchVM.IsActive && existingUserBranch != null)
                    {
                        // ≈“«·… ›—⁄ „‰ «·„” Œœ„
                        _context.UserBranches.Remove(existingUserBranch);
                    }
                }

                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage(" „  ÕœÌÀ ’·«ÕÌ«  «·›—Ê⁄ »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÕœÀ Œÿ√ √À‰«¡ «·Õ›Ÿ: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (userComboBox.SelectedItem != null)
            {
                var selectedUserName = userComboBox.SelectedItem as User;
                var selectedUser = _users.FirstOrDefault(u => u.Id == selectedUserName.Id);

                if (selectedUser != null)
                {
                    await LoadPermissionsForUserAsync(selectedUser);
                }
            }
        }

        public async Task LoadPermissionsForUserAsync(User user)
        {
            try
            {
                // ≈⁄«œ…  ⁄ÌÌ‰ Ã„Ì⁄ «·›—Ê⁄
                foreach (var branch in _branches)
                {
                    branch.IsActive = false;
                }

                // «·Õ’Ê· ⁄·Ï «·›—Ê⁄ «·„”„ÊÕ »Â« ··„” Œœ„
                var userBranches = await _context.UserBranches
                    .Where(ub => ub.UserID == user.Id)
                    .Include(ub => ub.Branch)
                    .Select(ub => ub.Branch.Id)
                    .ToListAsync();

                //  ›⁄Ì· «·›—Ê⁄ «·„”„ÊÕ »Â«
                foreach (var branchId in userBranches)
                {
                    var branchVM = _branches.FirstOrDefault(b => b.Id == branchId);
                    if (branchVM != null)
                    {
                        branchVM.IsActive = true;
                    }
                }

                //  ÕœÌÀ DataGrid
                dataTable.Items.Refresh();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·’·«ÕÌ« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            saveData();
        }

        private void All_check_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var branch in _branches)
            {
                branch.IsActive = true;
            }
            dataTable.Items.Refresh();
        }

        private void All_check_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var branch in _branches)
            {
                branch.IsActive = false;
            }
            dataTable.Items.Refresh();
        }
    }
}
