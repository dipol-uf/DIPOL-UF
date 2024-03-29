﻿using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace DIPOL_UF.Converters 
{
    class CameraStatsToStringValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
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
                Helper.WriteLog(value?.ToString());

                return value?.ToString() ?? "";
            }
            else
                return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
