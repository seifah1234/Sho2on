using Sho2on.Database;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
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
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window, INotifyPropertyChanged
    {

        private string connectionString = App.ConnectionString;
        public event PropertyChangedEventHandler PropertyChanged;

        private Color _primaryColor = Colors.Blue; // Default #007bff
        private Color _secondaryColor = Colors.Blue;
        private Color _ThirdColorBrush = Colors.Blue;
        private Color _primaryColorBackgroundColor = Colors.Blue; // Default #007bff
        private Color _mainMenuColorColor = Colors.Blue;
        private Color _primaryTextBrush = Colors.Blue;
        private Color _secondaryTextBrush = Colors.Blue;
        private string _primaryColorHex = "#007BFF";
        private string _secondaryColorHex = "#007BFF";
        private string _ThirdColorBrushHex = "#007BFF";
        private string _primaryColorBackgroundColorHex = "#ececec";
        private string _mainMenuColorHex = "#0047ab";
        private string _primaryTextBrushHex = "#0047ab";
        private string _secondaryTextBrushHex = "#F0FFF0";

        public Color PrimaryColor
        {
            get => _primaryColor;
            set { _primaryColor = value; UpdateColorResource("PrimaryColor", value); OnPropertyChanged(nameof(PrimaryColor)); }
        }

        public Color SecondaryColor
        {
            get => _secondaryColor;
            set { _secondaryColor = value; UpdateColorResource("SecondaryColor", value); OnPropertyChanged(nameof(SecondaryColor)); }
        }

        public Color ThirdColorBrush
        {
            get => _ThirdColorBrush;
            set { _ThirdColorBrush = value; UpdateColorResource("ThirdColorBrush", value); OnPropertyChanged(nameof(ThirdColorBrush)); }
        }

        public Color PrimaryColorBackground
        {
            get => _primaryColorBackgroundColor;
            set { _primaryColorBackgroundColor = value; UpdateColorResource("PrimaryColorBackground", value); OnPropertyChanged(nameof(PrimaryColorBackground)); }
        }

        public Color MainMenuColor
        {
            get => _mainMenuColorColor;
            set { _mainMenuColorColor = value; UpdateColorResource("MainMenuColor", value); OnPropertyChanged(nameof(MainMenuColor)); }
        }

        public Color PrimaryTextBrush
        {
            get => _primaryTextBrush;
            set { _primaryTextBrush = value; UpdateColorResource("PrimaryTextBrush", value); OnPropertyChanged(nameof(PrimaryTextBrush)); }
        }

        public Color SecondaryTextBrush
        {
            get => _secondaryTextBrush;
            set { _secondaryTextBrush = value; UpdateColorResource("SecondaryTextBrush", value); OnPropertyChanged(nameof(SecondaryTextBrush)); }
        }

        public string PrimaryColorHex
        {
            get => _primaryColorHex;
            set
            {
                if (TryParseHexColor(value, out Color color))
                {
                    _primaryColorHex = value;
                    PrimaryColor = color;
                    OnPropertyChanged(nameof(PrimaryColorHex));
                }
                else
                {
                    LocalizationManager.ShowMessage("تنسيق لون غير صالح. استخدم تنسيق #RRGGBB",
                       LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public string SecondaryColorHex
        {
            get => _secondaryColorHex;
            set
            {
                if (TryParseHexColor(value, out Color color))
                {
                    _secondaryColorHex = value;
                    SecondaryColor = color;
                    OnPropertyChanged(nameof(SecondaryColorHex));
                }
                else
                {
                    LocalizationManager.ShowMessage("تنسيق لون غير صالح. استخدم تنسيق #RRGGBB",
                       LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public string ThirdColorBrushHex
        {
            get => _ThirdColorBrushHex;
            set
            {
                if (TryParseHexColor(value, out Color color))
                {
                    _ThirdColorBrushHex = value;
                    ThirdColorBrush = color;
                    OnPropertyChanged(nameof(ThirdColorBrushHex));
                }
                else
                {
                    LocalizationManager.ShowMessage("تنسيق لون غير صالح. استخدم تنسيق #RRGGBB",
                        LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        public string PrimaryColorBackgroundHex
        {
            get => _primaryColorBackgroundColorHex;
            set
            {
                if (TryParseHexColor(value, out Color color))
                {
                    _primaryColorBackgroundColorHex = value;
                    PrimaryColorBackground = color;
                    OnPropertyChanged(nameof(PrimaryColorBackgroundHex));
                }
                else
                {
                    LocalizationManager.ShowMessage("تنسيق لون غير صالح. استخدم تنسيق #RRGGBB",
                       LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public string MainMenuColorHex
        {
            get => _mainMenuColorHex;
            set
            {
                if (TryParseHexColor(value, out Color color))
                {
                    _mainMenuColorHex = value;
                    MainMenuColor = color;
                    OnPropertyChanged(nameof(MainMenuColorHex));
                }
                else
                {
                    LocalizationManager.ShowMessage("تنسيق لون غير صالح. استخدم تنسيق #RRGGBB",
                         LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public string PrimaryTextBrushHex
        {
            get => _primaryTextBrushHex;
            set
            {
                if (TryParseHexColor(value, out Color color))
                {
                    _primaryTextBrushHex = value;
                    PrimaryTextBrush = color;
                    OnPropertyChanged(nameof(PrimaryTextBrushHex));
                }
                else
                {
                    LocalizationManager.ShowMessage("تنسيق لون غير صالح. استخدم تنسيق #RRGGBB",
                       LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public string SecondaryTextBrushHex
        {
            get => _secondaryTextBrushHex;
            set
            {
                if (TryParseHexColor(value, out Color color))
                {
                    _secondaryTextBrushHex = value;
                    SecondaryTextBrush = color;
                    OnPropertyChanged(nameof(SecondaryTextBrushHex));
                }
                else
                {
                    LocalizationManager.ShowMessage("تنسيق لون غير صالح. استخدم تنسيق #RRGGBB",
                        LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public Settings()
        {
            InitializeComponent();
            DataContext = this;

            LoadData();
        }

        private void UpdateColorResource(string key, Color color)
        {
            // نحدد الـ LightTheme.xaml
            var lightTheme = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(rd => rd.Source != null && rd.Source.OriginalString.Contains("LightTheme.xaml"));

            if (lightTheme != null)
            {
                lightTheme[key] = new SolidColorBrush(color);

                // نحفظ القيمة في Settings
                Properties.Settings.Default[key] = color;
                Properties.Settings.Default.Save();
            }
        }


        private bool TryParseHexColor(string hex, out Color color)
        {
            color = Colors.Black; // Default fallback
            if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || (hex.Length != 7 && hex.Length != 9))
                return false;

            try
            {
                color = (Color)ColorConverter.ConvertFromString(hex);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadData()
        {
            try
            {
                PrimaryColor = Properties.Settings.Default.PrimaryColor;
                SecondaryColor = Properties.Settings.Default.SecondaryColor;
                ThirdColorBrush = Properties.Settings.Default.ThirdColorBrush;
                PrimaryColorBackground = Properties.Settings.Default.PrimaryColorBackground;
                MainMenuColor = Properties.Settings.Default.MainMenuColor;
                PrimaryTextBrush = Properties.Settings.Default.PrimaryTextBrush;
                SecondaryTextBrush = Properties.Settings.Default.SecondaryTextBrush;

                PrimaryColorHex = ColorToHex(PrimaryColor);
                SecondaryColorHex = ColorToHex(SecondaryColor);
                ThirdColorBrushHex = ColorToHex(ThirdColorBrush);

                PrimaryColorBackgroundHex = ColorToHex(PrimaryColorBackground);
                MainMenuColorHex = ColorToHex(MainMenuColor);
                PrimaryTextBrushHex = ColorToHex(PrimaryTextBrush);
                SecondaryTextBrushHex = ColorToHex(SecondaryTextBrush);

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
                LocalizationManager.ShowMessage(e.Message);
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


            LocalizationManager.ShowMessage("Settings saved successfully!");
                
            
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

                    LocalizationManager.ShowMessage("Logo updated successfully!");
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"Error updating logo: {ex.Message}");
                }
            }
        }

        private void officalsForAllBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.OfficalsForAll = officalsForAllBox.IsChecked ?? false;
            Properties.Settings.Default.Save();


        }

        private void officalsForAllBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.OfficalsForAll = officalsForAllBox.IsChecked ?? false;
            Properties.Settings.Default.Save();

        }

        private void save_company_Btn_Click(object sender, RoutedEventArgs e)
        {
            AppDbContext dbContext = new AppDbContext(connectionString);
            var setting = dbContext.Settings.FirstOrDefault();
            if (setting != null)
            {
                setting.CompanyName = company_name_txt.Text;
                dbContext.SaveChanges();
                LocalizationManager.ShowMessage("Company information updated successfully!");
            }
        }
    }
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
                return new SolidColorBrush(color);
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

