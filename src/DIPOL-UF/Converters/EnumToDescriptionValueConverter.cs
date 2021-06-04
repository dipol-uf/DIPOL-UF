using System;
using System.Collections;
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
                IEnumerable enumVals => 
                    (
                            // This checks if target type implements IEnumerable
                            targetType.FindInterfaces((t, o) => o is Type compType && t == compType, typeof(IEnumerable)).Any(), 
                            ConverterImplementations.EnumToDescriptionConversion(enumVals.Cast<Enum>())
                    ) switch
                    {
                        (true, var values) => values,
                        var (_, values) => values?.EnumerableToString() 
                    },
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
