using System;
using System.Globalization;
using System.Windows.Data;

namespace DIPOL_UF.Converters
{
    class DefaultValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isNullVal = (value == null) || (value is string s && string.IsNullOrEmpty(s));

            if (!isNullVal)
                return value;
            else if (parameter != null)
                return parameter;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
