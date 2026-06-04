using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    public partial class Permissions : Window
    {
        private readonly AppDbContext _context = new AppDbContext(App.ConnectionString);
        private List<Role> _roles = new List<Role>();

        public Permissions()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
            DataContext = new MenuViewModel(_context);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRolesAsync();
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                roleComboBox.Items.Clear();
                _roles.Clear();

                _roles = _context.Roles
                    .OrderBy(r => r.RoleName)
                    .ToList();

                foreach (var role in _roles)
                {
                    roleComboBox.Items.Add(role.RoleName);
                }

                if (_roles.Any())
                {
                    roleComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل الأدوار: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadGIFAsync()
        {
            // يمكن إضافة تحميل GIF إذا لزم الأمر
            await Task.CompletedTask;
        }

        private async void SaveData()
        {
            try
            {
                if (roleComboBox.SelectedItem == null)
                {
                    LocalizationManager.ShowMessage("يرجى اختيار دور أولاً", LocalizationManager.Translate("تحذير"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var viewModel = (MenuViewModel)DataContext;
                var selectedItems = viewModel.GetSelectedItems();
                var selectedRoleName = roleComboBox.SelectedItem.ToString();
                var role = _roles.FirstOrDefault(r => r.RoleName == selectedRoleName);

                if (role == null)
                {
                    LocalizationManager.ShowMessage("الدور المحدد غير موجود", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // الحصول على الأذونات الحالية للدور
                var currentPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleID == role.RoleID)
                    .Include(rp => rp.Permission)
                    .ToListAsync();

                // معالجة الأذونات المحددة
                foreach (var (permissionName, isActive) in selectedItems)
                {
                    var permission = await _context.Permissions
                        .FirstOrDefaultAsync(p => p.PermissionName == permissionName);

                    if (permission == null)
                    {
                        // إنشاء إذن جديد إذا لم يكن موجوداً
                        permission = new Permission { PermissionName = permissionName };
                        await _context.Permissions.AddAsync(permission);
                        await _context.SaveChangesAsync();
                    }

                    var existingPermission = currentPermissions
                        .FirstOrDefault(rp => rp.Permission.PermissionName == permissionName);

                    if (isActive && existingPermission == null)
                    {
                        // إضافة إذن جديد
                        var rolePermission = new RolePermission
                        {
                            RoleID = role.RoleID,
                            PermissionID = permission.PermissionID
                        };
                        await _context.RolePermissions.AddAsync(rolePermission);
                    }
                    else if (!isActive && existingPermission != null)
                    {
                        // إزالة إذن موجود
                        _context.RolePermissions.Remove(existingPermission);
                    }
                }

                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("تم تحديث الأذونات بنجاح", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"حدث خطأ أثناء حفظ البيانات: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void roleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (roleComboBox.SelectedItem != null)
            {
                var selectedRoleName = roleComboBox.SelectedItem.ToString();
                var role = _roles.FirstOrDefault(r => r.RoleName == selectedRoleName);

                if (role != null)
                {
                    var viewModel = (MenuViewModel)DataContext;
                    await viewModel.LoadPermissionsForRoleAsync(role.RoleID);
                }
            }
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
        }
    }
}
