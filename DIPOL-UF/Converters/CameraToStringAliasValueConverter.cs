using System;
using System.Windows.Data;
using System.Linq;
using System.Globalization;
using System.Collections;
using ANDOR_CS.Classes;

namespace DIPOL_UF.Converters
{
    [ValueConversion(typeof(Camera), typeof(string))]
    class CameraToStringAliasValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Camera cam)
            {
                return ConverterImplementations.CameraToStringAliasConversion(cam);
            }
            else return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
