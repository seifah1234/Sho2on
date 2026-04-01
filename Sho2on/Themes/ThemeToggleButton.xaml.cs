using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.IconPacks;
using UserControl = System.Windows.Controls.UserControl;

namespace HR_Application.UserControls
{
    public partial class ThemeToggleButton : UserControl
    {
        public ThemeToggleButton()
        {
            InitializeComponent();
            UpdateIcon();
        }

        private void PART_ToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Toggle();
            UpdateIcon();
        }

        private void UpdateIcon()
        {
            // Sun  = currently dark  (click → go light)
            // Moon = currently light (click → go dark)
            ThemeIcon.Kind = ThemeManager.IsDark
                ? PackIconMaterialKind.WeatherSunny
                : PackIconMaterialKind.WeatherNight;
        }
    }
}
