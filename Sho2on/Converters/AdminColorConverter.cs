using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace HR_Application.Converters
{
    public class AdminColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAdmin && isAdmin)
                return new SolidColorBrush(Color.FromRgb(0, 188, 212)); // #00BCD4
            return new SolidColorBrush(Color.FromRgb(128, 128, 128)); // Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AdminIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAdmin && isAdmin)
                return "Crown";
            return "Account";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AdminIconColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAdmin && isAdmin)
                return new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Gold
            return new SolidColorBrush(Color.FromRgb(128, 128, 128)); // Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AdminToggleTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAdmin && isAdmin)
                return "إزالة صلاحية المشرف";
            return "جعله مشرفاً";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}