using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace DIPOL_UF.Converters
{
    [ValueConversion(typeof(double), typeof(Thickness))]
    class MarginModifierValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double dVal &&
                parameter is Thickness thck)
                return new Thickness(thck.Left * dVal, thck.Top * dVal, thck.Right * dVal, thck.Bottom * dVal);

            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
