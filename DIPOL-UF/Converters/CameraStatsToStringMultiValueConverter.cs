using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace DIPOL_UF.Converters 
{
    class CameraStatsToStringMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var value = ConvertWorker(values, targetType, parameter, culture);

            if (value is float)
                return ((float)value).ToString("#0.00");
            else return value?.ToString() ?? "";
        }

        internal object ConvertWorker(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 &&
                values[0] is ObservableConcurrentDictionary<string, Dictionary<string, object>> statsCollection &&
                values[1] is string &&
                statsCollection.ContainsKey((string)values[1]))
            {
                Dictionary<string, object> cameraStats = statsCollection[(string)values[1]];

                object value = parameter is string paramStr
                    && cameraStats != null
                    && cameraStats.ContainsKey(paramStr)
                    ? cameraStats[paramStr]
                    : null;

                return value ?? null;
            }
            else
                return null;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
