using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

using ANDOR_CS.Enums;

namespace DIPOL_UF.Converters
{
    class TemperatureStatusToBrushMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var value = new CameraStatsToValueMultiValueConverter().ConvertWorker(values, targetType, "TempStatus", culture);

            if (value is TemperatureStatus status && parameter is Brush[])
            {
                var brushes = parameter as Brush[];
                if (brushes.Length >= 5)

                    switch (status)
                    {
                        case TemperatureStatus.Off:
                            return brushes[0];
                        case TemperatureStatus.Stabilized:
                            return brushes[1];
                        case TemperatureStatus.NotReached:
                            return brushes[2];
                        case TemperatureStatus.Drift:
                            return brushes[3];
                        case TemperatureStatus.NotStabilized:
                            return brushes[4];
                    }
            }

            return Brushes.Black;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
