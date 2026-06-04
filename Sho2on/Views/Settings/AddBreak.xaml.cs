using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
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
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for AddBreak.xaml
    /// </summary>
    public partial class AddBreak : Window
    {

        public AddBreak()
        {
            InitializeComponent();
            LoadData();
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


        private void LoadData()
        {
            try
            {
                using (var db = new AppDbContext(App.ConnectionString))
                {
                    list.ItemsSource = db.Breaks
                        .OrderBy(b => b.StartTime)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage(ex.Message);
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
                if (FromTimePicker.SelectedDateTime == null || ToTimePicker.SelectedDateTime == null)
                {
                    LocalizationManager.ShowMessage("«Œ «— Êﬁ  «·—«Õ…!", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var from = FromTimePicker.SelectedDateTime.Value;
                var to = ToTimePicker.SelectedDateTime.Value;

                var newBreak = new Break
                {
                    Name = $"{from:HH:mm} - {to:HH:mm}",
                    StartTime = from.TimeOfDay,
                    EndTime = to.TimeOfDay,
                    EditedAt = DateTime.Now
                };

                using (var db = new AppDbContext(App.ConnectionString))
                {
                    db.Breaks.Add(newBreak);
                    db.SaveChanges();
                }

                LocalizationManager.ShowMessage(" „ ≈÷«›… › —… «·—«Õ…");

                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÕœÀ Œÿ√");
            }
        }

        private void exit_Btn(object sender, EventArgs e)
        {
            Close();
        }

        private void delete_Btn(object sender, EventArgs e)
        {
            if (list.SelectedItem == null)
            {
                LocalizationManager.ShowMessage("«Œ «— › —… «·—«Õ…");
                return;
            }

            try
            {
                string selected = list.SelectedItem.ToString();

                using (var db = new AppDbContext(App.ConnectionString))
                {
                    var br = db.Breaks.FirstOrDefault(x => x.Name == selected);
                    if (br != null)
                    {
                        db.Breaks.Remove(br);
                        db.SaveChanges();
                    }
                }

                LocalizationManager.ShowMessage(" „ «·Õ–›");
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÕœÀ Œÿ√");
            }
        }

        private void edit_Btn(object sender, EventArgs e)
        {
            if (list.SelectedItem == null)
            {
                LocalizationManager.ShowMessage("«Œ «— › —… «·—«Õ…!");
                return;
            }

            var from = FromTimePicker.SelectedDateTime.Value;
            var to = ToTimePicker.SelectedDateTime.Value;

            try
            {
                using (var db = new AppDbContext(App.ConnectionString))
                {
                    string selected = list.SelectedItem.ToString();

                    var br = db.Breaks.FirstOrDefault(x => x.Name == selected);

                    if (br != null)
                    {
                        br.Name = $"{from:HH:mm} - {to:HH:mm}";
                        br.StartTime = from.TimeOfDay;
                        br.EndTime = to.TimeOfDay;

                        db.SaveChanges();
                    }
                }

                LocalizationManager.ShowMessage(" „ «· ⁄œÌ·");
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÕœÀ Œÿ√");
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

