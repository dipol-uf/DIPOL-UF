using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;

namespace DIPOL_UF
{
    
    //[MarkupExtensionReturnType(typeof(string))]
    public class TextExtension : MarkupExtension
    {
        private Tuple<DependencyObject, DependencyProperty> _dependencyObjectInfo;
        /// <summary>
        /// Raised when application localization is changed through <see cref="UpdateUICullture"/>.
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
        public static void UpdateUICullture(CultureInfo newCulture)
        {
            if(Application.Current?.Dispatcher?.Thread == null)
                throw new NullReferenceException("Application is not properly initialized.");

            var old = Application.Current.Dispatcher.Thread.CurrentUICulture;
            Application.Current.Dispatcher.Thread.CurrentUICulture = newCulture;
            Helper.ExecuteOnUI(() => 
                LocalizationChanged?.Invoke(null, 
                    new Tuple<CultureInfo, CultureInfo>(old, newCulture)));
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
          
            if (_dependencyObjectInfo == null)
            {
                var ipvt = (IProvideValueTarget) serviceProvider.GetService(typeof(IProvideValueTarget));
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
                var value = Properties.Localization.ResourceManager.GetString(Key) ?? Key;

                return string.Format(GetFormat(), value);
            }

            return null;
        }

    }
}
