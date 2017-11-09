using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

namespace DIPOL_UF.Converters
{
    class TemperatureToBrushMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var value = new CameraStatsToStringMultiValueConverter().ConvertWorker(values, targetType, "Temp", culture);

            if (value is float temp && parameter is Brush[])
            {
                var brushes = parameter as Brush[];
                int size = brushes.Length;

                if (temp > 20)
                    return size > 0 ? brushes[0] : Brushes.Black;
                else if (temp > 5)
                    return size > 1 ? brushes[1] : Brushes.Black;
                else if (temp > -15)
                    return size > 2 ? brushes[2] : Brushes.Black;
                else
                    return size > 3 ? brushes[3] : Brushes.Black;
            }

            return Brushes.Black;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
