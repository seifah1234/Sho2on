using Sho2on.Database;
using Sho2on.Database.Models;
using System; 
using HR_Application.Helpers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; 
using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for AddDepart.xaml
    /// </summary>
    /// 

    public partial class AddRole : Window
    {

        private List<Role> _roles = new List<Role>();

        public AddRole()
        {
            InitializeComponent();

        }

        private async Task LoadData()
        {
            try
            {
                name_box.Clear();

                using(var db = new AppDbContext(App.ConnectionString))
                {
                    _roles = await db.Roles.ToListAsync();
                   list.ItemsSource = _roles;
                }

                editBtn.Visibility = Visibility.Collapsed;
                saveBtn.Visibility = Visibility.Visible;

            }
            catch (Exception e)
            {
                LocalizationManager.ShowMessage(e.Message);
            }

        }

        private void Exit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }


        private async void save_Btn(object sender, EventArgs e)
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
                    if (_roles.FirstOrDefault(r =>  r.RoleName == role.RoleName) == null)
                    {
                        LocalizationManager.ShowMessage("هذا الجروب موجود بالفعل", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    await db.Roles.AddAsync(role);
                    await db.SaveChangesAsync();
                }
                LocalizationManager.ShowMessage("تم اضافة الجروب", "", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();

            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void exit_Btn(object sender, EventArgs e)
        {
            Close();
        }

       

        private async void edit_Btn(object sender, EventArgs e)
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
                            await db.SaveChangesAsync();
                        }
                    }
                    LocalizationManager.ShowMessage("تم تعديل الجروب", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadData();
                }
                else
                {
                    LocalizationManager.ShowMessage("لم تختار اي جروب", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (list.SelectedItem is Role role)
            {
                
                name_box.Text = role.RoleName;
                editBtn.Visibility = Visibility.Visible;
                saveBtn.Visibility = Visibility.Collapsed;


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

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            list.SelectedItem = null;
            name_box.Clear();
            editBtn.Visibility = Visibility.Collapsed;
            saveBtn.Visibility = Visibility.Visible;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }
    }
}

