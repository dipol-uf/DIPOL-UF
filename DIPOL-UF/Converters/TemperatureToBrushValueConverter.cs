using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

namespace DIPOL_UF.Converters
{
    class TemperatureToBrushValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        =>
            ConverterImplementations.TemperatureToBrushConversion(
                value is float temp ? temp : 0f,
                parameter is Brush[] brushes ? brushes : null);
        

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
