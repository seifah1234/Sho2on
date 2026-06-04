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
                LocalizationManager.ShowMessage($"ÎØÃ Ýí ÊÍãíá ÇáÃÏæÇÑ: {ex.Message}", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadGIFAsync()
        {
            // íãßä ÅÖÇÝÉ ÊÍãíá GIF ÅÐÇ áÒã ÇáÃãÑ
            await Task.CompletedTask;
        }

        private async void SaveData()
        {
            try
            {
                if (roleComboBox.SelectedItem == null)
                {
                    LocalizationManager.ShowMessage("íÑÌì ÇÎÊíÇÑ ÏæÑ ÃæáÇð", "ÊÍÐíÑ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var viewModel = (MenuViewModel)DataContext;
                var selectedItems = viewModel.GetSelectedItems();
                var selectedRoleName = roleComboBox.SelectedItem.ToString();
                var role = _roles.FirstOrDefault(r => r.RoleName == selectedRoleName);

                if (role == null)
                {
                    LocalizationManager.ShowMessage("ÇáÏæÑ ÇáãÍÏÏ ÛíÑ ãæÌæÏ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ÇáÍÕæá Úáì ÇáÃÐæäÇÊ ÇáÍÇáíÉ ááÏæÑ
                var currentPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleID == role.RoleID)
                    .Include(rp => rp.Permission)
                    .ToListAsync();

                // ãÚÇáÌÉ ÇáÃÐæäÇÊ ÇáãÍÏÏÉ
                foreach (var (permissionName, isActive) in selectedItems)
                {
                    var permission = await _context.Permissions
                        .FirstOrDefaultAsync(p => p.PermissionName == permissionName);

                    if (permission == null)
                    {
                        // ÅäÔÇÁ ÅÐä ÌÏíÏ ÅÐÇ áã íßä ãæÌæÏÇð
                        permission = new Permission { PermissionName = permissionName };
                        await _context.Permissions.AddAsync(permission);
                        await _context.SaveChangesAsync();
                    }

                    var existingPermission = currentPermissions
                        .FirstOrDefault(rp => rp.Permission.PermissionName == permissionName);

                    if (isActive && existingPermission == null)
                    {
                        // ÅÖÇÝÉ ÅÐä ÌÏíÏ
                        var rolePermission = new RolePermission
                        {
                            RoleID = role.RoleID,
                            PermissionID = permission.PermissionID
                        };
                        await _context.RolePermissions.AddAsync(rolePermission);
                    }
                    else if (!isActive && existingPermission != null)
                    {
                        // ÅÒÇáÉ ÅÐä ãæÌæÏ
                        _context.RolePermissions.Remove(existingPermission);
                    }
                }

                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("Êã ÊÍÏíË ÇáÃÐæäÇÊ ÈäÌÇÍ", "äÌÇÍ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÍÏË ÎØÃ ÃËäÇÁ ÍÝÙ ÇáÈíÇäÇÊ: {ex.Message}", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
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
