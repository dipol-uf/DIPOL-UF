//    This file is part of Dipol-3 Camera Manager.

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
using System.Collections;
using System.Collections.Generic;
using System.Windows.Data;
using System.Globalization;


namespace DIPOL_UF.Converters
{
    class ValueTupleToValueValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string fieldName)
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

                return ConverterImplementations.FieldAccessConversion(value, fieldName);


            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException(string.Format(
                Properties.Localization.General_OperationNotSupported,
                $"{nameof(ValueTupleToValueValueConverter)}.{nameof(ConvertBack)}"));
    }
}
