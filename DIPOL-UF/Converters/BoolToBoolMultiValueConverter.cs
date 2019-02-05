using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using DIPOL_UF.Properties;


namespace DIPOL_UF.Converters
{
    internal class BoolToBoolMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool GetBool(object val)
                => val is bool b && b;

            if (targetType == typeof(bool))
                ConverterImplementations.BoolToBoolConversion(
                    values.Select(GetBool).ToList(),
                    parameter as string);
           
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException(string.Format(
                Localization.General_OperationNotSupported,
                $"{nameof(BoolToBoolMultiValueConverter)}.{nameof(ConvertBack)}"));
    }
}
