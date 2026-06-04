// Converters/PercentageConverter.cs
using System; using HR_Application.Helpers;
using System.Windows.Data;

namespace HR_Application.Converters
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is decimal percentage)
            {
                return $"{percentage:F1}%";
            }
            else if (value is double doublePercentage)
            {
                return $"{doublePercentage:F1}%";
            }
            else if (value is int intPercentage)
            {
                return $"{intPercentage}%";
            }
            return "0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}