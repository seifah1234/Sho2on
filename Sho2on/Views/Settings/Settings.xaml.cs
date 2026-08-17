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
        private Color _secondaryColor;
        private Color _accentColor;
        private Color _sidebarColor;
        private Color _primaryColor;
        private Color _textPrimaryColor;
        private Color _textSecondaryColor;
        private Color _thirdColor;

        public Color SecondaryColor
        {
            get => _secondaryColor;
            set { _secondaryColor = value; OnPropertyChanged(nameof(SecondaryColor)); }
        }

        public Color AccentColor
        {
            get => _accentColor;
            set { _accentColor = value; OnPropertyChanged(nameof(AccentColor)); }
        }

        public Color SidebarColor
        {
            get => _sidebarColor;
            set { _sidebarColor = value; OnPropertyChanged(nameof(SidebarColor)); }
        }
        public Color PrimaryColor
        {
            get => _primaryColor;
            set { _primaryColor = value; OnPropertyChanged(nameof(PrimaryColor)); }
        }
        public Color TextPrimaryColor
        {
            get => _textPrimaryColor;
            set { _textPrimaryColor = value; OnPropertyChanged(nameof(TextPrimaryColor)); }
        }
        public Color TextSecondaryColor
        {
            get => _textSecondaryColor;
            set { _textSecondaryColor = value; OnPropertyChanged(nameof(TextSecondaryColor)); }
        }
        public Color ThirdColor
        {
            get => _thirdColor;
            set { _thirdColor = value; OnPropertyChanged(nameof(ThirdColor)); }
        }

        public Settings()
        {
            InitializeComponent();
            LoadSavedColors();
            DataContext = this;
            LoadData();
        }

        private void LoadSavedColors()
        {
            _accentColor = ParseColor(Properties.Settings.Default.AccentColor, "#0097A7");
            _sidebarColor = ParseColor(Properties.Settings.Default.SidebarColor, "#004D56");
            _thirdColor = ParseColor(Properties.Settings.Default.ThirdColorBrush, "#00838F");
            _primaryColor = ParseColor(Properties.Settings.Default.PrimaryColor, "#FFFFFF");
            _textPrimaryColor = ParseColor(Properties.Settings.Default.PrimaryTextBrush, "#1A3C40");
            _secondaryColor = ParseColor(Properties.Settings.Default.SecondaryColor, "#006064");
            _textSecondaryColor = ParseColor(Properties.Settings.Default.SecondaryTextBrush, "#37474F");
        }

        private Color ParseColor(string hex, string fallback)
        {
            try
            {
                if (!string.IsNullOrEmpty(hex) && hex.StartsWith("#"))
                    return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch { }
            return (Color)ColorConverter.ConvertFromString(fallback);
        }

        private string ColorToHex(Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        private void UpdateResource(string key, Color color)
        {
            var lightTheme = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(rd => rd.Source != null &&
                                rd.Source.OriginalString.Contains("LightTheme.xaml"));

            if (lightTheme != null)
                lightTheme[key] = new SolidColorBrush(color);
        }

        private void SaveColors_Click(object sender, RoutedEventArgs e)
        {
            // تحديث الـ Resources فوراً
            UpdateResource("SecondaryColor", _secondaryColor);
            UpdateResource("InputBackground", _thirdColor); // نفس اللون
            UpdateResource("PrimaryColor", _primaryColor);
            UpdateResource("AccentColor", _accentColor);
            UpdateResource("TextPrimaryColor", _textPrimaryColor);
            UpdateResource("TextSecondaryColor", _textSecondaryColor);

            // تحديث الـ SidebarBrush (LinearGradient)
            var lightTheme = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(rd => rd.Source != null &&
                                rd.Source.OriginalString.Contains("LightTheme.xaml"));

            if (lightTheme != null)
            {
                var sidebarBrush = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(0, 1)
                };
                sidebarBrush.GradientStops.Add(new GradientStop(_sidebarColor, 0));
                sidebarBrush.GradientStops.Add(
                    new GradientStop(DarkenColor(_sidebarColor, 0.8), 1));
                lightTheme["SidebarBrush"] = sidebarBrush;
            }

            // حفظ في Settings
            Properties.Settings.Default.SecondaryColor = ColorToHex(_secondaryColor);
            Properties.Settings.Default.PrimaryColor = ColorToHex(_primaryColor);
            Properties.Settings.Default.PrimaryTextBrush = ColorToHex(_textPrimaryColor);
            Properties.Settings.Default.ThirdColorBrush = ColorToHex(_thirdColor);
            Properties.Settings.Default.AccentColor = ColorToHex(_accentColor);
            Properties.Settings.Default.SidebarColor = ColorToHex(_sidebarColor);
            Properties.Settings.Default.SecondaryTextBrush = ColorToHex(_sidebarColor);
            Properties.Settings.Default.SecondaryTextBrush = ColorToHex(_textSecondaryColor);
            Properties.Settings.Default.Save();

            LocalizationManager.ShowMessage("تم حفظ الالوان!");
        }

        private Color DarkenColor(Color color, double factor)
        {
            return Color.FromRgb(
                (byte)(color.R * factor),
                (byte)(color.G * factor),
                (byte)(color.B * factor));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private void OfficialsForAllBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.OfficialsForAll = OfficialsForAllBox.IsChecked ?? false;
            Properties.Settings.Default.Save();


        }

        private void OfficialsForAllBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.OfficialsForAll = OfficialsForAllBox.IsChecked ?? false;
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

