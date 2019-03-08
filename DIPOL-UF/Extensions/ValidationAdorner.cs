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

using System.Windows;
using System.Windows.Controls;

namespace DIPOL_UF.Extensions
{
    internal class ValidationAdorner : DependencyObject
    {
        public static readonly DependencyProperty AdornerProperty 
        = DependencyProperty.RegisterAttached(@"Adorner", typeof(bool), typeof(ValidationAdorner),
            new PropertyMetadata(default(bool), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool b && b)
                Validation.AddErrorHandler(d, Handler);
            else
                Validation.RemoveErrorHandler(d, Handler);
        }

        private static void Handler(object sender, ValidationErrorEventArgs e)
        {
            if (!(sender is DependencyObject obj)) return;

            if (Validation.GetHasError(obj) is var hasErrors && hasErrors)
            {
                ToolTipService.SetToolTip(obj,
                    Converters.ConverterImplementations.ValidationErrorsToStringConversion(
                        Validation.GetErrors(obj)));
            }
            else
                ToolTipService.SetToolTip(obj, null);
        }

        public static void SetAdorner(DependencyObject element, bool value)
            => element.SetValue(AdornerProperty, value);

        public static bool GetAdorner(DependencyObject element)
            => (bool)element.GetValue(AdornerProperty);
    }
}
