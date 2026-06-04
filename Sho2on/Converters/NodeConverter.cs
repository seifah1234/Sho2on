using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace HR_Application.Converters
{
        public class NodeColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var type = value?.ToString();
                return type switch
                {
                    "الشركة" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00416A")),
                    "قطاع" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#004687")),
                    "فرع" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#082567")),
                    "ادارة" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003153")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"))
                };
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }
        public class NodeHeightConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var type = value?.ToString();
                return type switch
                {
                    "ادارة" => 45.0,
                    "الشركة" => 60.0,
                    "قطاع" => 55.0,
                    "فرع" => 50.0,
                    _ => 20.0
                };
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }

        public class NodeIconConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var type = value?.ToString();
                return type switch
                {
                    "الشركة" => "🏢",
                    "فرع" => "🏬",
                    "ادارة" => "🏛️",
                    "قطاع" => "🏢",
                    "قسم" => "👤",
                    _ => "📁"
                };
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }

        public class VisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is int count && count > 0)
                    return System.Windows.Visibility.Visible;
                return System.Windows.Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }
    
}
