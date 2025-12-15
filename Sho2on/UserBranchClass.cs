using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;

namespace HR_Application
{
    public class UserBranchClass
    {
        public ObservableCollection<Item> Items { get; set; }

        private Dictionary<string, ObservableCollection<SubSubItem>> menus = new Dictionary<string, ObservableCollection<SubSubItem>>();

        public static bool IsAuto = true;

        public UserBranchClass()
        {
            SubMenu();
            Items = new ObservableCollection<Item>();
            foreach (var menu in menus)
            {

                var i = new Item
                {
                    SubItems = new ObservableCollection<SubItem>
                        {
                            new SubItem
                            {
                                SubItemName = menu.Key,
                                SubSubItems = menu.Value
                            }
                        },
                    IsActive = true
                };
                Items.Add(i);
            }
        }

        public void LoadPermissionsForRole(int roleId)
        {
            IsAuto = false;
            foreach (var item in Items)
            {
                foreach (var subItem in item.SubItems)
                {
                    subItem.IsActive = false; // Reset all items to inactive
                    foreach (var subSubItem in subItem.SubSubItems)
                    {
                        subSubItem.IsActive = false; // Reset all sub-items to inactive
                    }
                }
            }

            using (SqlConnection connection = new SqlConnection(App.ConnectionString))
            {
                connection.Open();
                string query = @"
            SELECT p.Name
            FROM UserBranches rp
            JOIN t_branch p ON rp.BranchCode = p.Code
            WHERE rp.UserID = @UserID";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", roleId);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        string permissionName = reader["Name"].ToString();

                        // Set IsActive to true for the matching permission
                        foreach (var item in Items)
                        {
                            foreach (var subItem in item.SubItems)
                            {

                                foreach (var subSubItem in subItem.SubSubItems)
                                {
                                    if (subSubItem.SubSubItemName == permissionName)
                                    {
                                        subSubItem.IsActive = true;
                                    }
                                }

                                if (subItem.SubItemName == permissionName)
                                {
                                    subItem.IsActive = true;
                                }
                            }
                        }
                    }
                }
            }
            IsAuto = true;
        }


        private void SubMenu()
        {

            SqlConnection connection = new SqlConnection(App.ConnectionString);
            connection.Open();
            string query = @"SELECT Code, Name FROM t_branch";
            SqlCommand cmd = new SqlCommand(query, connection);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                if (!menus.ContainsKey(reader["Name"].ToString()))
                {
                    ObservableCollection<SubSubItem> subMenu = new ObservableCollection<SubSubItem>();
                    subMenu.Add(new SubSubItem { SubSubItemName = reader["Code"].ToString() });
                    menus.Add(reader["Name"].ToString(), subMenu);
                }
                else
                {
                    menus[reader["Name"].ToString()].Add(new SubSubItem { SubSubItemName = reader["Code"].ToString() });
                }
            }
        }

        public class Item : INotifyPropertyChanged
        {
            private bool _isActive;

            public ObservableCollection<SubItem> SubItems { get; set; }

            public bool IsActive
            {
                get => _isActive;
                set
                {
                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                    // Propagate the checked state to subitems
                    foreach (var subItem in SubItems)
                    {
                        subItem.IsActive = value;
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public class SubItem : INotifyPropertyChanged
        {
            private bool _isActive;

            public string SubItemName { get; set; }
            public ObservableCollection<SubSubItem> SubSubItems { get; set; }

            public bool IsActive
            {
                get => _isActive;
                set
                {
                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                    // Propagate the checked state to subsubitems
                    if (IsAuto)
                    {
                        foreach (var subSubItem in SubSubItems)
                        {
                            subSubItem.IsActive = value;
                        }

                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public List<(string Name, bool IsActive)> GetSelectedItems()
        {
            var selectedItems = new List<(string Name, bool IsActive)>();

            foreach (var item in Items)
            {
                foreach (var subItem in item.SubItems)
                {
                    selectedItems.Add((subItem.SubItemName, subItem.IsActive));

                    foreach (var subSubItem in subItem.SubSubItems)
                    {
                        selectedItems.Add((subSubItem.SubSubItemName, subSubItem.IsActive));
                    }
                }
            }

            return selectedItems;
        }


        public class SubSubItem : INotifyPropertyChanged
        {
            private bool _isActive;

            public string SubSubItemName { get; set; }

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
    }

}
