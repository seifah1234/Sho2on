using HR_Application.Helpers;
using HR_Application.Helpers;
using System; 
using System.Windows;
using System.Windows.Media;
using Xceed.Wpf.AvalonDock.Properties;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace HR_Application
{
    /// <summary>
    /// Swaps the active theme ResourceDictionary at runtime.
    /// Call ThemeManager.Toggle() from any Button Click handler.
    /// The choice is persisted in user settings so it survives restarts.
    /// </summary>
    public static class ThemeManager
    {
        private const string DarkThemeSource  = "Themes/DarkTheme.xaml";
        private const string LightThemeSource = "Themes/LightTheme.xaml";

        // ── Public API ────────────────────────────────────────────────

        public static bool IsDark => _currentSource == DarkThemeSource;

        /// <summary>Switches to the opposite theme.</summary>
        public static void Toggle()
        {
            ApplyTheme(_currentSource != DarkThemeSource);
        }

        /// <summary>Loads a specific theme ("Dark" or "Light").</summary>
        public static void Apply(string themeName)
        {
            ApplyTheme(themeName != "Light");
        }

        public static void ApplyTheme(bool isDarkTheme)
        {
            
            // Clear existing theme resources
            var existingTheme = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d =>
                d.Source?.OriginalString.Contains("Theme.xaml") == true);

            if (existingTheme != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(existingTheme);
            }

            // Add new theme
            var themeUri = isDarkTheme ? DarkThemeSource : LightThemeSource;
            var newTheme = new ResourceDictionary()
            {
                Source = new Uri(themeUri, UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Insert(0, newTheme);

            if (!isDarkTheme)
            {
                // ✅ Light → طبق الألوان اللي المستخدم مختارها
                LoadColorsFromSettings();
                _currentSource = LightThemeSource;
            }
            else
            {
                // ✅ Dark → امسح أي تأثير من Settings وخليها اللي في XAML
                ResetColorsToThemeDefaults();
                _currentSource = DarkThemeSource;
            }

            HR_Application.Properties.Settings.Default.ThemeName =
                _currentSource == LightThemeSource ? "Light" : "Dark";
            HR_Application.Properties.Settings.Default.Save();
        }


        private static void UpdateResource(string key, System.Windows.Media.Color color)
        {
            var lightTheme = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(rd => rd.Source != null &&
                                rd.Source.OriginalString.Contains("LightTheme.xaml"));

            if (lightTheme != null)
                lightTheme[key] = new SolidColorBrush(color);
        }

        private static System.Windows.Media.Color ParseColor(string hex, string fallback)
        {
            try
            {
                if (!string.IsNullOrEmpty(hex) && hex.StartsWith("#"))
                    return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch { }
            return (Color)ColorConverter.ConvertFromString(fallback);
        }

        public static void LoadColorsFromSettings()
        {
            try
            {
                // تحديث الـ Resources فوراً
                UpdateResource("SecondaryColor", ParseColor(HR_Application.Properties.Settings.Default.SecondaryColor, "#006064"));
                UpdateResource("InputBackground", ParseColor(HR_Application.Properties.Settings.Default.ThirdColorBrush, "#00838F")); // نفس اللون
                UpdateResource("PrimaryColor", ParseColor(HR_Application.Properties.Settings.Default.PrimaryColor, "#FFFFFF"));
                UpdateResource("AccentColor", ParseColor(HR_Application.Properties.Settings.Default.AccentColor, "#0097A7"));
                UpdateResource("TextPrimaryColor", ParseColor(HR_Application.Properties.Settings.Default.PrimaryTextBrush, "#1A3C40"));
                UpdateResource("TextSecondaryColor", ParseColor(HR_Application.Properties.Settings.Default.SecondaryTextBrush, "#37474F"));

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
                    sidebarBrush.GradientStops.Add(new GradientStop(ParseColor(HR_Application.Properties.Settings.Default.SidebarColor, "#004D56"), 0));
                    sidebarBrush.GradientStops.Add(
                        new GradientStop(DarkenColor(ParseColor(HR_Application.Properties.Settings.Default.SidebarColor, "#004D56"), 0.8), 1));
                    lightTheme["SidebarBrush"] = sidebarBrush;
                }
            }
            catch
            {
                // fallback default لو حصل مشكلة
                Application.Current.Resources["PrimaryColor"] = new SolidColorBrush(Colors.Blue);
                Application.Current.Resources["SecondaryColor"] = new SolidColorBrush(Colors.Gray);
                Application.Current.Resources["ThirdColorBrush"] = new SolidColorBrush(Colors.LightGray);
            }
        }
        private static Color DarkenColor(Color color, double factor)
        {
            return Color.FromRgb(
                (byte)(color.R * factor),
                (byte)(color.G * factor),
                (byte)(color.B * factor));
        }

        private static void ResetColorsToThemeDefaults()
        {
            try
            {
                Application.Current.Resources.Remove("PrimaryColor");
                Application.Current.Resources.Remove("SecondaryColor");
                Application.Current.Resources.Remove("ThirdColorBrush");
                Application.Current.Resources.Remove("InputBackground");
                Application.Current.Resources.Remove("BorderColor");
                Application.Current.Resources.Remove("TextPrimaryColor");
                Application.Current.Resources.Remove("TextSecondaryColor");
            }
            catch
            {
                // ignore
            }
        }


        /// <summary>
        /// Call once from App.OnStartup AFTER base.OnStartup(e),
        /// before any Window is shown, to restore the saved preference.
        /// </summary>
        public static void Initialize()
        {
            string saved = HR_Application.Properties.Settings.Default.ThemeName;
            ApplyTheme(saved == "Dark");
        }

        // ── Internal ──────────────────────────────────────────────────

        private static string _currentSource = DarkThemeSource;

        /*private static void ApplyTheme(string source)
        {
            _currentSource = source;

            var mergedDicts = Application.Current.Resources.MergedDictionaries;

            // Remove the existing theme dict (always first entry)
            if (mergedDicts.Count > 0)
                mergedDicts.RemoveAt(0);

            // Insert the new one at position 0
            var dict = new ResourceDictionary
            {
                Source = new Uri(source, UriKind.Relative)
            };
            mergedDicts.Insert(0, dict);

            // Persist
            HR_Application.Properties.Settings.Default.ThemeName =
                source == LightThemeSource ? "Light" : "Dark";
            HR_Application.Properties.Settings.Default.Save();
        }
  */
    }
}
