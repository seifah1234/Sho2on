using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
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
    /// Interaction logic for AddHoliType.xaml
    /// </summary>
    public partial class AddHoliType : Window
    {
        public AddHoliType()
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
                    var holidayTypes = db.HolidayTypes.ToList();
                    list.ItemsSource = holidayTypes.ToList();
                }
                
                
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

        private void B_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
            if (e.ClickCount == 2)
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


        private void save_Btn(object sender, EventArgs e)
        {

            try
            {
                SqlConnection con = new SqlConnection(App.ConnectionString);

                con.Open();
                string name = name_box.Text;
                using (var db = new AppDbContext(App.ConnectionString))
                {
                    var existingHolidayType = db.HolidayTypes.FirstOrDefault(ht => ht.Name == name);
                    if (existingHolidayType != null)
                    {
                        LocalizationManager.ShowMessage("نوع الاجازة موجود مسبقاً", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var holidayType = new HolidayType
                    {
                        Name = name
                    };
                    db.HolidayTypes.Add(holidayType);
                    db.SaveChanges();

                }
                LocalizationManager.ShowMessage("تم اضافة نوع الاجازة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();

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

        private void delete_Btn(object sender, EventArgs e)
        {
            if (list.SelectedItem is not HolidayType selectedHolidayType)
            {
                LocalizationManager.ShowMessage("لم يتم اختيار نوع الاجازة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                try
                {
                    using (var db = new AppDbContext(App.ConnectionString))
                    {
                        
                            db.HolidayTypes.Remove(selectedHolidayType);
                            db.SaveChanges();
                        
                    }
                    LocalizationManager.ShowMessage("تم حذف نوع الاجازة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                catch
                {
                    LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void edit_Btn(object sender, EventArgs e)
        {

            try
            {
                if (list.SelectedItem is HolidayType holidayType)
                {
                    
                    string name = name_box.Text;
                    holidayType.Name = name;
                    using (var db = new AppDbContext(App.ConnectionString))
                    {
                        db.HolidayTypes.Update(holidayType);
                        db.SaveChanges();
                    }

                    LocalizationManager.ShowMessage("تم تعديل نوع الاجازة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    LocalizationManager.ShowMessage("لم تختار اي نوع اجازة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {

            if (list.SelectedItem is HolidayType holidayType)
            {
                string query = "SELECT * FROM t_holidayType";
                SqlConnection con = new SqlConnection(App.ConnectionString);

               
                name_box.Text = holidayType.Name;
                            
                

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

