using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Data;
using System.Globalization;
using System.Linq;

namespace DIPOL_UF.Converters
{
    internal class EnumToDescriptionValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value switch
            {
                Enum enumVal => ConverterImplementations.EnumToDescriptionConversion(enumVal),
                IEnumerable enumVals => ConverterImplementations.EnumToDescriptionConversion(enumVals.Cast<Enum>()),
                _ => null
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            //=> value is string desc 
            //    ? ConverterImplementations.DescriptionToEnumConversion(desc, targetType) 
            //    : null;
            => throw new NotSupportedException(string.Format(
                Properties.Localization.General_OperationNotSupported,
                $"{nameof(EnumToDescriptionValueConverter)}.{nameof(ConvertBack)}"));

    }
}
