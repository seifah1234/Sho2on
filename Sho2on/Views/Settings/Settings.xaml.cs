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
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {

        private string connectionString = App.ConnectionString;

        public Settings()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                
                string month_data = "";
                
                 month_data = $"اعدادات بداية الشهر الحالية : {Properties.Settings.Default.StartOfMonth} و نهايته : {Properties.Settings.Default.EndOfMonth}";
                begin_month.Text = Properties.Settings.Default.StartOfMonth.ToString();
                end_month.Text = Properties.Settings.Default.EndOfMonth.ToString();
                month_detail_txt.FlowDirection = System.Windows.FlowDirection.RightToLeft;
                month_detail_txt.Content = month_data;

                if (!string.IsNullOrEmpty(Properties.Settings.Default.Logo))
                {
                    logo_path_txt.Content = Properties.Settings.Default.Logo;

                }
                else
                {
                    logo_path_txt.Content = "No logo selected";
                }

            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }

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


        private void Min_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

        private void save_month_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(begin_month.Text) && !string.IsNullOrEmpty(end_month.Text))
            {
                Properties.Settings.Default.StartOfMonth = Convert.ToInt32(begin_month.Text);
                Properties.Settings.Default.EndOfMonth = Convert.ToInt32(end_month.Text);
                Properties.Settings.Default.Save();
            }
            string month_data = $"اعدادات بداية الشهر الحالية : {Properties.Settings.Default.StartOfMonth} و نهايته : {Properties.Settings.Default.EndOfMonth}";

            month_detail_txt.FlowDirection = System.Windows.FlowDirection.RightToLeft;
            month_detail_txt.Content = month_data;


            System.Windows.MessageBox.Show("Settings saved successfully!");
                
            
        }

        private void upload_logo_btn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {

                    Properties.Settings.Default.Logo = openFileDialog.FileName;
                    Properties.Settings.Default.Save();
                    logo_path_txt.Content = openFileDialog.FileName;

                    MessageBox.Show("Logo updated successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating logo: {ex.Message}");
                }
            }
        }
    }
}
