using System;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace DIPOL_UF.Converters
{
    [ValueConversion(typeof(string), typeof(double))]
    [ValueConversion(typeof(string), typeof(float))]
    class StringToSingleDoubleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                if (targetType == typeof(double))
                {
                    if (double.TryParse(s, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out double dVal))
                        return dVal;
                    else return 0.0;
                }
                else if (targetType == typeof(float))
                {
                    if (float.TryParse(s, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out float fVal))
                        return fVal;
                    else return 0.0f;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double dVal)
            {
                if (parameter is string dFrmt && !string.IsNullOrWhiteSpace(dFrmt))
                    return dVal.ToString(dFrmt);
                else return dVal.ToString();
            }
            else if (value is float fVal)
            {
                if (parameter is string fFrmt && !string.IsNullOrWhiteSpace(fFrmt))
                    return fVal.ToString(fFrmt);
                else return fVal.ToString();
            }
            else return null;
        }
    }
}
