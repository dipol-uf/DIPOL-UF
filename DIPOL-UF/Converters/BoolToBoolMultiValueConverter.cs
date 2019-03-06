﻿//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Linq;
using System.Windows.Data;
using System.Globalization;
using System.Windows;
using Localization = DIPOL_UF.Properties.Localization;


namespace DIPOL_UF.Converters
{
    internal class BoolToBoolMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool GetBool(object val)
                => val is bool b && b;

            if (targetType == typeof(bool))
                return ConverterImplementations.BoolToBoolConversion(
                    values.Select(GetBool).ToList(),
                    parameter as string);

            if (parameter is IValueConverter outerConverter && targetType == typeof(Visibility))
            {
                return outerConverter.Convert(ConverterImplementations.BoolToBoolConversion(
                    values.Select(GetBool).ToList()), targetType, null, culture);
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException(string.Format(
                Localization.General_OperationNotSupported,
                $"{nameof(BoolToBoolMultiValueConverter)}.{nameof(ConvertBack)}"));
    }
}
