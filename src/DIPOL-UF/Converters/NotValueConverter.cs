﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace DIPOL_UF.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    internal class NotValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return null;
        }
    }
}
