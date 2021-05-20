using System;
using System.Globalization;
using System.Windows.Data;

namespace DIPOL_UF.Converters
{
    internal sealed class TextAsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => parameter is string s
                ? Properties.Localization.ResourceManager.GetString(s, CultureInfo.CurrentUICulture)
                : "";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException(string.Format(Properties.Localization.General_OperationNotSupported,
                $"{nameof(TextAsConverter)}.{nameof(ConvertBack)}"));

    }
}
