using System;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace DIPOL_UF.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolVal = (bool)value;
            if (boolVal)
                return Visibility.Visible;
            else
            {
                if (parameter is Visibility vis)
                    return vis;
                else
                    return Visibility.Hidden;
            }
                    
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visVal = (Visibility)value;

            switch (visVal)
            {
                case Visibility.Visible:
                    return true;
                case Visibility.Collapsed:
                    if (parameter is bool boolPar)
                        return boolPar;
                    return false;
                case Visibility.Hidden:
                default:
                    return false;
            }
        }
    }
}
