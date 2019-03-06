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
        private Tuple<DependencyObject, DependencyProperty> _dependencyObjectInfo;
        /// <summary>
        /// Raised when application localization is changed through <see cref="UpdateUiCulture"/>.
        /// </summary>
        public static event EventHandler<Tuple<CultureInfo, CultureInfo>> LocalizationChanged;

        /// <summary>
        /// Resource key used to lookup localized strings
        /// </summary>
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
            LocalizationChanged += Localization_Changed;
        }
       

        /// <summary>
        /// Updates UI culture and forces update of all localized strings.
        /// </summary>
        /// <param name="newCulture">Culture to switch to.</param>
        public static void UpdateUiCulture(CultureInfo newCulture)
        {
            if(Application.Current?.Dispatcher?.Thread == null)
                throw new NullReferenceException("Application is not properly initialized.");

            var old = Application.Current.Dispatcher.Thread.CurrentUICulture;
            Application.Current.Dispatcher.Thread.CurrentUICulture = newCulture;
            Helper.ExecuteOnUI(() => 
                LocalizationChanged?.Invoke(null, 
                    new Tuple<CultureInfo, CultureInfo>(old, newCulture)));
        }

        internal string GetText(string key)
        {
#if DEBUG
            if(_dependencyObjectInfo?.Item1 is DependencyObject obj
                && DesignerProperties.GetIsInDesignMode(obj))
                return Properties.Localization.ResourceManager.GetString(key);

#endif
            return Properties.Localization.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        }


        public override object ProvideValue(IServiceProvider serviceProvider)
        {
          
            if (_dependencyObjectInfo == null)
            {
                var ipvt = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
                if (ipvt?.TargetObject is DependencyObject depObj &&
                    ipvt.TargetProperty is DependencyProperty depProp)
                {
                    _dependencyObjectInfo = new Tuple<DependencyObject, DependencyProperty>(
                        depObj, depProp); 
                }
                
            }

            return GetValue();
        }

        protected virtual void Localization_Changed(object sender, Tuple<CultureInfo, CultureInfo> e)
        {
                _dependencyObjectInfo?.Item1?.SetValue(_dependencyObjectInfo.Item2, GetValue());
        }

        protected virtual string GetFormat()
        {
            if (Format is string strFormat)
                return strFormat;
            if  (Format is TextExtension ext)
                return ext.GetValue();
            throw new ArgumentException();
        }

        protected virtual string GetValue()
        {
            if (Key != null)
            {
                var value = GetText(Key) ?? Key;

                return string.Format(GetFormat(), value);
            }

            return null;
        }

    }
}
