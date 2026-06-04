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
                LocalizationManager.ShowMessage($"خطأ في تحميل البيانات: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                LocalizationManager.ShowMessage($"خطأ في تحميل المستخدمين: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                LocalizationManager.ShowMessage($"خطأ في تحميل الفروع: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                    LocalizationManager.ShowMessage("يرجى اختيار مستخدم أولاً", LocalizationManager.Translate("تحذير"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedUserName = userComboBox.SelectedItem as User;
                if (selectedUserName == null)
                {
                    LocalizationManager.ShowMessage("المستخدم المحدد غير صالح", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var selectedUser = _users.FirstOrDefault(u => u.Id == selectedUserName.Id);

                if (selectedUser == null)
                {
                    LocalizationManager.ShowMessage("المستخدم المحدد غير موجود", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // الحصول على الفروع الحالية للمستخدم
                var currentUserBranches = await _context.UserBranches
                    .Where(ub => ub.UserID == selectedUser.Id)
                    .ToListAsync();

                // معالجة الفروع المحددة
                foreach (var branchVM in _branches)
                {
                    var existingUserBranch = currentUserBranches
                        .FirstOrDefault(ub => ub.BranchId == branchVM.Id);

                    if (branchVM.IsActive && existingUserBranch == null)
                    {
                        // إضافة فرع جديد للمستخدم
                        var userBranch = new UserBranch
                        {
                            UserID = selectedUser.Id,
                            BranchId = branchVM.Id,
                        };
                        await _context.UserBranches.AddAsync(userBranch);
                    }
                    else if (!branchVM.IsActive && existingUserBranch != null)
                    {
                        // إزالة فرع من المستخدم
                        _context.UserBranches.Remove(existingUserBranch);
                    }
                }

                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("تم تحديث صلاحيات الفروع بنجاح", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"حدث خطأ أثناء الحفظ: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                // إعادة تعيين جميع الفروع
                foreach (var branch in _branches)
                {
                    branch.IsActive = false;
                }

                // الحصول على الفروع المسموح بها للمستخدم
                var userBranches = await _context.UserBranches
                    .Where(ub => ub.UserID == user.Id)
                    .Include(ub => ub.Branch)
                    .Select(ub => ub.Branch.Id)
                    .ToListAsync();

                // تفعيل الفروع المسموح بها
                foreach (var branchId in userBranches)
                {
                    var branchVM = _branches.FirstOrDefault(b => b.Id == branchId);
                    if (branchVM != null)
                    {
                        branchVM.IsActive = true;
                    }
                }

                // تحديث DataGrid
                dataTable.Items.Refresh();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل الصلاحيات: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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
