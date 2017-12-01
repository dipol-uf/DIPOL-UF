using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace DIPOL_UF.Converters
{
    class EnumToDescriptionValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(IEnumerable) && value is Array enumVals)
            {
                if (enumVals.Length > 0)
                {
                    var enumType = enumVals.GetValue(0).GetType();

                    string[] names = new string[enumVals.Length];

                    for (int i = 0; i < names.Length; i++)
                        names[i] = Helper.GetEnumDescription(enumVals.GetValue(i), enumType);

                    return names;

                }
                else
                    return new[] { "" };
            }
            else if (value is Enum enumVal)
                return Helper.GetEnumDescription(enumVal, enumVal.GetType());
           
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string desc)
            {
                Type type; ;

                if (targetType.BaseType == typeof(Enum))
                    return Helper.GetEnumFromDescription(desc, targetType);
                else if ((type = Nullable.GetUnderlyingType(targetType)) != null &&
                    type.BaseType == typeof(Enum))
                    return Helper.GetEnumFromDescription(desc, type);

            }
            return null;
        }
    }
}
