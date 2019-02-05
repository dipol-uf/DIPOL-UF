using System;
using System.Globalization;
using System.Windows.Data;

namespace DIPOL_UF.Converters
{
    internal class NotValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && targetType == typeof(bool))
                return !b;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && targetType == typeof(bool))
                return !b;
            return null;
        }
    }
}
