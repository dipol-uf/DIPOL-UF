using System;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace DIPOL_UF.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    class CameraKeyToStringValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = (value as string)?.Split(':')[0] ?? "Unknown";
            if (val.ToLowerInvariant() == "localhost")
                val = "Local";

            return val + ":";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
