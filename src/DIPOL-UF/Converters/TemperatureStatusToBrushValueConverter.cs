using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

using ANDOR_CS.Enums;

namespace DIPOL_UF.Converters
{
    internal sealed class TemperatureStatusToBrushValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value is TemperatureStatus status 
                && parameter is Brush[] brushes)
            return ConverterImplementations.TemperatureStatusToBrushConversion(status, brushes);

            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(
                string.Format(
                    Properties.Localization.General_OperationNotSupported,
                    $"{nameof(TemperatureStatusToBrushValueConverter)}.{nameof(ConvertBack)}"));
        }
    }
}
