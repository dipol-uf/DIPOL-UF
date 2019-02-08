using System;
using System.Configuration;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Navigation;

namespace DIPOL_UF.Converters
{
    internal class DebugConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            => values;

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => new[] {value};
    }
}

