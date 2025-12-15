using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
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
                MessageBox.Show(ex.Message);
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
                    MessageBox.Show("اختار وقت الراحة!", "", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                MessageBox.Show("تم إضافة فترة الراحة");

                LoadData();
            }
            catch
            {
                MessageBox.Show("حدث خطأ");
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
                MessageBox.Show("اختار فترة الراحة");
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

                MessageBox.Show("تم الحذف");
                LoadData();
            }
            catch
            {
                MessageBox.Show("حدث خطأ");
            }
        }

        private void edit_Btn(object sender, EventArgs e)
        {
            if (list.SelectedItem == null)
            {
                MessageBox.Show("اختار فترة الراحة!");
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

                MessageBox.Show("تم التعديل");
                LoadData();
            }
            catch
            {
                MessageBox.Show("حدث خطأ");
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
