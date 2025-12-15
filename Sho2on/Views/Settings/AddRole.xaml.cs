using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for AddDepart.xaml
    /// </summary>
    /// 

    public partial class AddRole : Window
    {


        public AddRole()
        {
            InitializeComponent();
            LoadData();

        }

        private void LoadData()
        {
            try
            {
                name_box.Clear();

                using(var db = new AppDbContext(App.ConnectionString))
                {
                   list.ItemsSource = db.Roles.ToList();
                }

              
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }

        }

        private void Exit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }


        private void save_Btn(object sender, EventArgs e)
        {

            try
            {
                
                string name = name_box.Text;
                using (var db = new AppDbContext(App.ConnectionString))
                {
                    var role = new Sho2on.Database.Models.Role
                    {
                        RoleName = name
                    };
                    db.Roles.Add(role);
                    db.SaveChanges();
                }
                System.Windows.MessageBox.Show("تم اضافة الجروب", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();

            }
            catch
            {
                System.Windows.MessageBox.Show("حدث خطأ", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void exit_Btn(object sender, EventArgs e)
        {
            Close();
        }

       

        private void edit_Btn(object sender, EventArgs e)
        {

            try
            {
                if (list.SelectedItem is Role role)
                {
                    
                    string name = name_box.Text;
                    using (var db = new AppDbContext(App.ConnectionString))
                    {
                        var existingRole = db.Roles.Find(role.RoleID);
                        if (existingRole != null)
                        {
                            existingRole.RoleName = name;
                            db.SaveChanges();
                        }
                    }
                    System.Windows.MessageBox.Show("تم تعديل الجروب", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    System.Windows.MessageBox.Show("لم تختار اي جروب", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch
            {
                System.Windows.MessageBox.Show("حدث خطأ", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (list.SelectedItem is Role role)
            {
                
                name_box.Text = role.RoleName;
                 
                
            }

        }


        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Max_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {

                this.WindowState = WindowState.Maximized;
            }
        }
    }
}
