using System;
using System.Windows.Data;
using System.Reflection;
using System.Globalization;


namespace DIPOL_UF.Converters
{
    class ValueTupleToValueValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = value.GetType();
            if (type.IsValueType && parameter is string fieldName)
                return type
                    .GetField(fieldName, BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(value);
            else return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Backward convertion is not supported");
        }
    }
}
