using System; using HR_Application.Helpers;
using System.Windows; using HR_Application.Helpers;
using Application = System.Windows.Application;

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
            ApplyTheme(_currentSource == DarkThemeSource
                ? LightThemeSource
                : DarkThemeSource);
        }

        /// <summary>Loads a specific theme ("Dark" or "Light").</summary>
        public static void Apply(string themeName)
        {
            ApplyTheme(themeName == "Light" ? LightThemeSource : DarkThemeSource);
        }

        /// <summary>
        /// Call once from App.OnStartup AFTER base.OnStartup(e),
        /// before any Window is shown, to restore the saved preference.
        /// </summary>
        public static void Initialize()
        {
            string saved = HR_Application.Properties.Settings.Default.ThemeName;
            ApplyTheme(saved == "Light" ? LightThemeSource : DarkThemeSource);
        }

        // ── Internal ──────────────────────────────────────────────────

        private static string _currentSource = DarkThemeSource;

        private static void ApplyTheme(string source)
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
    }
}
