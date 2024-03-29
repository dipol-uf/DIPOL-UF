﻿using System;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace DIPOL_UF.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    internal class CameraKeyToStringValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string s ? ConverterImplementations.CameraKeyToHostConversion(s) : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
