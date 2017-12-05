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
            var type = value?.GetType();
            if (type != null && parameter is string fieldName)
            {
                if (type.BaseType == typeof(Array))
                {
                    var array = value as Array;
                    Type innerType;
                    FieldInfo fi;
                    if (array.Length > 0 &&
                        (innerType = array.GetValue(0).GetType()).IsValueType &&
                        (fi = innerType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance)) != null)
                    {
                        Array result = Array.CreateInstance(fi.FieldType, array.Length);
                        for (int i = 0; i < array.Length; i++)
                            result.SetValue(fi.GetValue(array.GetValue(i)), i);

                        return result;
                    }
                }
                else if (type.IsValueType)
                    return type
                        .GetField(fieldName, BindingFlags.Public | BindingFlags.Instance)
                        ?.GetValue(value);

                
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Backward convertion is not supported");
        }
    }
}
