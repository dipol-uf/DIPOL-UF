using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DIPOL_UF
{
    static class Helper
    {
        public static Size MeasureString(string strToMeasure, TextBlock presenter)
        {
            var formattedText = new FormattedText(strToMeasure,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    presenter.FontFamily,
                    presenter.FontStyle,
                    presenter.FontWeight,
                    presenter.FontStretch),
                presenter.FontSize,
                presenter.Foreground,
                VisualTreeHelper.GetDpi(presenter).PixelsPerDip);

            return new Size(formattedText.Width, formattedText.Height);
        }

        public static DependencyObject FindParentOfType<T>(DependencyObject element)
        {
            var parent = VisualTreeHelper.GetParent(element);

            while(parent != null && !(parent is T))
                parent = VisualTreeHelper.GetParent(parent);

            return parent is T ? parent : null;
        }

        public static DependencyObject FindParentByName(DependencyObject element, string name)
        {
            var parent = VisualTreeHelper.GetParent(element);

            while ((parent as FrameworkElement)?.Name != name)
                parent = VisualTreeHelper.GetParent(parent);

            return (parent is FrameworkElement e && e.Name == name) ? parent : null;
        }

        public static bool IsDialogWindow(Window window)
        {
            var showingAsDialogField = window.GetType().GetField("_showingAsDialog", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            return (bool)(showingAsDialogField?.GetValue(window) ?? false);
        }

        public static void WriteLog(string entry)
        {
            System.Diagnostics.Debug.WriteLine(entry);
        }
        public static void WriteLog(ANDOR_CS.Exceptions.AndorSDKException entry)
        {
            System.Diagnostics.Debug.WriteLine($"{entry.Message} [{entry.ErrorCode}]");
        }
        public static void WriteLog(object entry)
            => WriteLog(entry.ToString());

        public static string GetCameraHostName(string input)
        {
            string host = input.Split(':')[0];
            if (!String.IsNullOrWhiteSpace(host))
                return host.ToLowerInvariant() == "localhost" ? "Local" : host;
            else
                return String.Empty;
        }

        /// <summary>
        /// Checks if dispatcher has not been shut down yet.
        /// </summary>
        /// <param name="d">Dispatcher instance (usually <see cref="Application.Current.Dispatcher"/>)</param>
        /// <returns>True if Dispatcher.Invoke can still be called.</returns>
        public static bool IsAvailable(this Dispatcher d)
            => !d.HasShutdownStarted && !d.HasShutdownFinished;

        /// <summary>
        /// Retrieves an item from a dictionary if specified key is present; otherwise, returns null.
        /// </summary>
        /// <param name="settings">The dictionary.</param>
        /// <param name="key">Key.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <returns>Either item associated with key, or null, if not present.</returns>
        public static object GetValueOrNullSafe(this Dictionary<string, object> settings, string key)
            => (settings ?? throw new ArgumentNullException($"{nameof(settings)} argument cannot be null."))
                .TryGetValue(key, out object item) ? item : null;


        public static T GetValueOrNullSafe<T>(this Dictionary<string, object> settings, string key, T nullReplacement = default(T))
        {
            var value = GetValueOrNullSafe(settings, key);

            if (value is T)
                return (T)value;
            else
                return nullReplacement;
        }

        public static string ArrayToString(this Array array)
        {
            var enumer = array.GetEnumerator();

            var s = new StringBuilder();

            if (enumer.MoveNext())
                s.Append(enumer.Current.ToString());

            while (enumer.MoveNext())
                s.Append(", " + enumer.Current.ToString());

            return s.ToString();
        }

        /// <summary>
        /// Gets value of <see cref="DescriptionAttribute"/> for an item from a given enum.
        /// </summary>
        /// <param name="enumValue">Value from the enum.</param>
        /// <param name="enumType">Enum type. If types mismatch, returns name of the field or null.</param>
        /// <returns></returns>
        public static string GetEnumDescription(object enumValue, Type enumType)
        {
            // Retrieves string representation of the enum value
            string fieldName = Enum.GetName(enumType, enumValue);

            // Which corresponds to field name of Enum-derived class
            var descriptionAttr = enumType
                .GetField(fieldName)
                // It is possible that such field is not defined (type error), from this point return null
                ?.GetCustomAttributes(typeof(DescriptionAttribute), false) 
                .DefaultIfEmpty(null)
                .FirstOrDefault();

            // Casts result to DescriptionAttribute. 
            // If attribute is not found or value is of wrong type, cast gives null and method returns fieldName
            return (descriptionAttr as DescriptionAttribute)?.Description ?? (fieldName ?? "");
            
        }

        /// <summary>
        /// Gets value of <see cref="Enum"/> for a given value of <see cref="DescriptionAttribute"/>.
        /// </summary>
        /// <param name="description">Description string as defined in attribute constructor.</param>
        /// <param name="enumType">Enum type.</param>
        /// <returns></returns>
        public static object GetEnumFromDescription(string description, Type enumType)
        {
            // Gets all declared enum values
            Array values = Enum.GetValues(enumType);
            
            // If any present
            if (values.Length > 0)
            {
                // Itereates through values, retrieves description for each
                for (int i = 0; i < values.Length; i++)
                {
                    var local = values.GetValue(i);
                    // If retrieved description equals to passed argument, returns this value.
                    if (GetEnumDescription(local, enumType) == description)
                        return local;
                }    
            }

            // If Enum is empty or description is not found/defined, returns null
            return null;
        }

    }
}
