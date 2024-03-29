﻿using System;
using System.Windows.Data;
using System.Globalization;

namespace DIPOL_UF.Converters
{
    /// <summary>
    /// Performs basic cast operations between types.
    /// Utilizes <see cref="System.Convert"/>
    /// </summary>
    class TypeValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
                return System.Convert.ChangeType(value, targetType);
            if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);
            return null;
        }

        /// <summary>
        /// Converts back. Works when binding is TwoWay or OneWayToSource.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Type.</param>
        /// <param name="parameter">Optional parameter.</param>
        /// <param name="culture">Culture information.</param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Convert(value, targetType, parameter, culture);

    }
}
