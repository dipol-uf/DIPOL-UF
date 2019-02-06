using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using DIPOL_UF.Properties;

namespace DIPOL_UF.Converters
{
    internal class ValidationErrorsToStringValueConverter :IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is IReadOnlyCollection<ValidationError> collection
                ? collection.Select(x => x.ErrorContent).EnumerableToString(";\r\n") + "."
                : null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException(
                string.Format(Localization.General_OperationNotSupported,
                    $"{nameof(ValidationErrorsToStringValueConverter)}.{nameof(ConvertBack)}"));
    }
}
