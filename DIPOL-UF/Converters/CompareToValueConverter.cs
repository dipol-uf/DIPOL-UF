using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using DIPOL_UF.Properties;

namespace DIPOL_UF.Converters
{
    class CompareToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => ConverterImplementations.CompareToConversion(value, parameter);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException(string.Format(Localization.General_OperationNotSupported,
                $"{nameof(CompareToValueConverter)}.{nameof(ConvertBack)}"));
    }
}
