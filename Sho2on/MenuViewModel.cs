using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    public class MenuViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _context;
        public ObservableCollection<MenuItem> Items { get; set; } = new ObservableCollection<MenuItem>();

        public static bool IsAuto { get; set; } = true;

        public MenuViewModel(AppDbContext context)
        {
            _context = context;
            _ = LoadMenuDataAsync();
        }

        private async Task LoadMenuDataAsync()
        {
            try
            {
                LoadMenuStructureAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات القائمة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadMenuStructureAsync()
        {
            var menus = _context.Menus
                .Include(m => m.Children) // تحميل الأطفال بشكل صريح
                .OrderBy(m => m.ParentId)
                .ThenBy(m => m.Name)
                .ToList();

            // إنشاء هيكل القائمة الهرمي
            var menuLookup = menus.ToLookup(m => m.ParentId);
            var rootMenus = menuLookup[null].ToList(); // القوائم الرئيسية (ParentId = null)

            Items.Clear();

            foreach (var rootMenu in rootMenus)
            {
                var menuItem = CreateMenuItem(rootMenu, menuLookup);
                Items.Add(menuItem);
            }
        }

        private MenuItem CreateMenuItem(Menu menu, ILookup<int?, Menu> menuLookup)
        {
            var menuItem = new MenuItem
            {
                Id = menu.Id,
                Name = menu.Name,
                IsActive = false
            };

            // إضافة القوائم الفرعية بشكل متكرر
            var childMenus = menuLookup[menu.Id].ToList();
            foreach (var childMenu in childMenus)
            {
                var childMenuItem = CreateMenuItem(childMenu, menuLookup);
                menuItem.Children.Add(childMenuItem);
            }

            return menuItem;
        }

        public async Task LoadPermissionsForRoleAsync(int roleId)
        {
            IsAuto = false;
            await ResetPermissionsAsync();

            try
            {
                var rolePermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleID == roleId)
                    .Include(rp => rp.Permission)
                    .Select(rp => rp.Permission.PermissionName)
                    .ToListAsync();

                foreach (var permissionName in rolePermissions)
                {
                    await SetPermissionActiveAsync(permissionName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الصلاحيات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            IsAuto = true;
        }

        private async Task ResetPermissionsAsync()
        {
            foreach (var item in Items)
            {
                await ResetMenuItemAsync(item);
            }
        }

        private async Task ResetMenuItemAsync(MenuItem menuItem)
        {
            menuItem.IsActive = false;
            foreach (var child in menuItem.Children)
            {
                await ResetMenuItemAsync(child);
            }
        }

        private async Task SetPermissionActiveAsync(string permissionName)
        {
            foreach (var item in Items)
            {
                if (await SetMenuItemActiveAsync(item, permissionName))
                {
                    break;
                }
            }
        }

        private async Task<bool> SetMenuItemActiveAsync(MenuItem menuItem, string permissionName)
        {
            if (menuItem.Name == permissionName)
            {
                menuItem.IsActive = true;
                return true;
            }

            foreach (var child in menuItem.Children)
            {
                if (await SetMenuItemActiveAsync(child, permissionName))
                {
                    return true;
                }
            }

            return false;
        }

        public List<(string Name, bool IsActive)> GetSelectedItems()
        {
            var selectedItems = new List<(string Name, bool IsActive)>();

            foreach (var item in Items)
            {
                CollectMenuItems(item, selectedItems);
            }

            return selectedItems;
        }

        private void CollectMenuItems(MenuItem menuItem, List<(string Name, bool IsActive)> selectedItems)
        {
            selectedItems.Add((menuItem.Name, menuItem.IsActive));

            foreach (var child in menuItem.Children)
            {
                CollectMenuItems(child, selectedItems);
            }
        }

        public List<Menu> GetAllMenus()
        {
            var allMenus = new List<Menu>();

            foreach (var item in Items)
            {
                CollectAllMenus(item, allMenus);
            }

            return allMenus;
        }

        private void CollectAllMenus(MenuItem menuItem, List<Menu> allMenus)
        {
            allMenus.Add(new Menu { Id = menuItem.Id, Name = menuItem.Name });

            foreach (var child in menuItem.Children)
            {
                CollectAllMenus(child, allMenus);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class MenuItem : INotifyPropertyChanged
        {
            private bool _isActive;

            public int Id { get; set; }
            public string Name { get; set; }
            public ObservableCollection<MenuItem> Children { get; set; } = new ObservableCollection<MenuItem>();

            public bool IsActive
            {
                get => _isActive;
                set
                {
                    if (_isActive != value)
                    {
                        _isActive = value;
                        OnPropertyChanged(nameof(IsActive));

                        if (IsAuto)
                        {
                            // تفعيل/إلغاء تفعيل جميع العناصر الفرعية
                            foreach (var child in Children)
                            {
                                child.IsActive = value;
                            }
                        }
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}