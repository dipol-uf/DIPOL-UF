using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    
    [MarkupExtensionReturnType(typeof(string))]
    public class TextExtension : MarkupExtension
    {
        private DependencyObject _targetObject;
        private DependencyProperty _targetProperty;

        /// <summary>
        /// Raised when application localization is changed through <see cref="UpdateUICullture"/>.
        /// </summary>
        public static event EventHandler<Tuple<CultureInfo, CultureInfo>> LocalizationChanged;

        /// <summary>
        /// Resource key used to lookup localized strings
        /// </summary>
        [ConstructorArgument("key")]
        public string Key
        {
            get;
            set;
        }

        public string Format
        {
            get;
            set;
        } = @"{0}";

        public TextExtension()
        {
            LocalizationChanged += Localization_Changed;
        }
        public TextExtension(object key)
        {
            if (key is string s)
                Key = s;
            else throw new ArgumentException("[Key] is required.");
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
            if (_targetObject == null || _targetProperty == null)
            {
                var ipvt = (IProvideValueTarget) serviceProvider.GetService(typeof(IProvideValueTarget));

                if (ipvt?.TargetObject is DependencyObject depObj &&
                    ipvt.TargetProperty is DependencyProperty depProp)
                {
                    _targetObject = depObj;
                    _targetProperty = depProp;
                }
            }

            return GetValue();
        }

        protected virtual void Localization_Changed(object sender, Tuple<CultureInfo, CultureInfo> e)
        {
            if(_targetProperty != null)
                _targetObject?.SetValue(_targetProperty, GetValue());
        }

        protected virtual string GetValue() 
            => string.Format(Format, Properties.Localization.ResourceManager.GetString(Key) ?? Key);

    }
}
