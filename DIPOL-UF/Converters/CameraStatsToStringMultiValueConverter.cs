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
            var value = new CameraStatsToValueMultiValueConverter().ConvertWorker(values, targetType, parameter, culture);

            if (value is Enum en)
            {
                var resourceName = $"General_{en.GetType().Name}_{en}";
                var localizedText = Properties.Localization.ResourceManager.GetString(resourceName);
                return localizedText ?? Helper.GetEnumDescription(en, en.GetType());
            }

            if (value is float f)
                return f.ToString("#0.00");
            return value?.ToString() ?? "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
