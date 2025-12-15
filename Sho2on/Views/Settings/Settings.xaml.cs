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
        int type;

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
                delay_repeat.Text = Properties.Settings.Default.LateRepeat.ToString();
                delay_value.Text = Properties.Settings.Default.LateValue.ToString();
                month_detail_txt.FlowDirection = System.Windows.FlowDirection.RightToLeft;
                month_detail_txt.Content = month_data;
                if(Properties.Settings.Default.LateType.ToString() == "1")
                {
                    moneyBtn.IsChecked = true;
                    minuteBtn.IsChecked = false;
                }
                else
                {
                    minuteBtn.IsChecked = true;
                    moneyBtn.IsChecked = false;
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
            }
                string month_data = $"اعدادات بداية الشهر الحالية : {Properties.Settings.Default.StartOfMonth} و نهايته : {Properties.Settings.Default.EndOfMonth}";

                month_detail_txt.FlowDirection = System.Windows.FlowDirection.RightToLeft;
                month_detail_txt.Content = month_data;

                Properties.Settings.Default.LateType = type;
                Properties.Settings.Default.LateValue = decimal.Parse(delay_value.Text);
                Properties.Settings.Default.LateRepeat = int.Parse(delay_repeat.Text);
                Properties.Settings.Default.Save();

                System.Windows.MessageBox.Show("Settings saved successfully!");
                
            
        }

        private void minuteBtn_Checked(object sender, RoutedEventArgs e)
        {
            type = 0;
        }

        private void moneyBtn_Checked(object sender, RoutedEventArgs e)
        {
            type = 1;
        }
    }
}
