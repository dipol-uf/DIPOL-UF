using System;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace DIPOL_UF.Converters
{
    class TypeValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
                return System.Convert.ChangeType(value, targetType);
            else if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);
            else return null;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
                return System.Convert.ChangeType(value, targetType);
            else if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);
            else return null; 
        }
    }
}
