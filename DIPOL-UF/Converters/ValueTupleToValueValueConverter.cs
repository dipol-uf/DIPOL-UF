using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Data;
using System.Reflection;
using System.Globalization;


namespace DIPOL_UF.Converters
{
    class ValueTupleToValueValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string fieldName && 
                value ?.GetType() is Type type)
            {
                if (value is IEnumerable collection)
                {
                    var enumer = collection.GetEnumerator();
                    var result = new List<object>();
                    while (enumer.MoveNext())
                    {
                        var item = ConverterImplementations.FieldAccessConversion(enumer.Current, fieldName);
                        result.Add(item);
                    }

                    var array = Array.CreateInstance(
                        result.Count > 0 ? result[0].GetType() : typeof(object),
                        result.Count);

                    for (var i = 0; i < array.Length; i++)
                        array.SetValue(result[i], i);

                    return array;

                }
                else return ConverterImplementations.FieldAccessConversion(value, fieldName);


            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException(string.Format(
                Properties.Localization.General_OperationNotSupported,
                $"{nameof(ValueTupleToValueValueConverter)}.{nameof(ConvertBack)}"));
    }
}
