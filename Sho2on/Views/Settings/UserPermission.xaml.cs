using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
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
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                roleComboBox.Items.Clear();
                _roles.Clear();

                // تحميل الأدوار من قاعدة البيانات
                var roles = await _context.Roles
                    .OrderBy(r => r.RoleName)
                    .ToListAsync();

                // إضافة عنصر "لا شيء"
                _roles.Add(new Role { RoleID = 0, RoleName = "لا شيء" });

                // إضافة الأدوار الأخرى
                foreach (var role in roles)
                {
                    _roles.Add(role);
                }

                // تعيين مصدر البيانات للكومبوبوكس
                roleComboBox.ItemsSource = _roles;
                roleComboBox.DisplayMemberPath = "RoleName";
                roleComboBox.SelectedValuePath = "RoleID";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الأدوار: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                _usersList.Clear();

                // تحميل المستخدمين الذين لديهم صلاحية IsUser = true
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
                        // الحصول على الدور الأول إذا كان موجوداً
                        RoleID = user.UserRoles.FirstOrDefault()?.RoleId ?? 0,
                        Password = user.PasswordHash
                    };

                    _usersList.Add(userVM);
                }

                dataTable.ItemsSource = _usersList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المستخدمين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // تعيين الدور المحدد
                roleComboBox.SelectedValue = selectedUser.RoleID;

                // تعيين كلمة المرور (يمكن إظهارها بشكل مشفر إذا لزم الأمر)
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
                    MessageBox.Show("يرجى اختيار مستخدم واحد", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (roleComboBox.SelectedItem is not Role selectedRole)
                {
                    MessageBox.Show("يرجى اختيار دور", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // الحصول على المستخدم من قاعدة البيانات
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == selectedUser.Id);

                if (user == null)
                {
                    MessageBox.Show("المستخدم غير موجود", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // تحديث كلمة المرور إذا تم إدخالها
                if (!string.IsNullOrEmpty(pass_box.Password))
                {
                    user.PasswordHash = pass_box.Password;
                }

                // تحديث كلمة المرور إذا تم إدخالها
                if (!string.IsNullOrEmpty(username_box.Text))
                {
                    user.Username = username_box.Text;
                }

                // إدارة أدوار المستخدم
                await ManageUserRolesAsync(user, selectedRole.RoleID);

                await _context.SaveChangesAsync();

                MessageBox.Show("تم تحديث صلاحيات المستخدم بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                // إعادة تحميل البيانات لتحديث الواجهة
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ManageUserRolesAsync(User user, int roleId)
        {
            // إزالة جميع الأدوار الحالية للمستخدم
            var currentUserRoles = user.UserRoles.ToList();
            _context.UserRoles.RemoveRange(currentUserRoles);

            // إضافة الدور الجديد إذا لم يكن "لا شيء"
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

        // دالة مساعدة لتشفير كلمة المرور (يمكن إضافتها إذا لزم الأمر)
        private string HashPassword(string password)
        {
            // يمكن استخدام مكتبة مثل BCrypt.Net هنا
            return password; // هذا مثال بسيط، في التطبيق الحقيقي يجب تشفير كلمة المرور
        }
    }
}