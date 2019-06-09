using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace DIPOL_UF.Converters
{
    internal class CombineMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            => values.ToList();

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException(string.Format(
                Properties.Localization.General_OperationNotSupported,
                $"{nameof(CombineMultiValueConverter)}.{nameof(ConvertBack)}"));
    }
}
