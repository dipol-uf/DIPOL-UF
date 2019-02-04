using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Globalization;
using System.Linq;

namespace DIPOL_UF.Converters
{
    internal class EnumToDescriptionValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case IEnumerable<Enum> enumVals:
                    return enumVals.Select(ConverterImplementations.EnumToDescriptionConversion)
                                   .ToList();
                case Enum enumVal:
                    return ConverterImplementations.EnumToDescriptionConversion(enumVal);
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string desc 
                ? ConverterImplementations.DescriptionToEnumConversion(desc, targetType) 
                : null;

    }
}
