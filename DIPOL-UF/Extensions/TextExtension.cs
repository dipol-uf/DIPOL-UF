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
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace DIPOL_UF.Extensions
{
    
    //[MarkupExtensionReturnType(typeof(string))]
    public class TextExtension : MarkupExtension
    {
        /// <summary>
        /// Resource key used to lookup localized strings
        /// </summary>
        [ConstructorArgument(@"key")]
        public string Key
        {
            get;
            set;
        }
        public object Format
        {
            get;
            set;
        } = @"{0}";

        public TextExtension()
        {
        }

        public TextExtension(string key)
            => Key = key;

       
        internal string GetText(string key, CultureInfo info = null)
            => Properties.Localization.ResourceManager.GetString(key, info ?? CultureInfo.CurrentUICulture);


        public override object ProvideValue(IServiceProvider serviceProvider)
        {

#if DEBUG
                var ipvt = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
                if (ipvt?.TargetObject is DependencyObject depObj)
                {
                    if (DesignerProperties.GetIsInDesignMode(depObj))
                        return Properties.Localization.ResourceManager.GetString(Key, CultureInfo.InvariantCulture);
                }
#endif


            return GetValue();
        }

        protected virtual string GetFormat()
        {
            if (Format is string strFormat)
                return strFormat;
            if  (Format is TextExtension ext)
                return ext.GetValue();
            throw new ArgumentException();
        }

        protected virtual string GetValue(CultureInfo info = null)
        {
            if (Key != null)
            {
                var value = GetText(Key, info) ?? Key;

                return string.Format(GetFormat(), value);
            }

            return null;
        }

    }
}
