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
            if(_dependencyObjectInfo.Item1 is DependencyObject obj
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
