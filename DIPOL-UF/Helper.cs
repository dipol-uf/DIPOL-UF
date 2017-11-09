using System;
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

            while(!(parent is T))
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
        /// Retrieves an item from a dictionary if specified key is present; otherwise, returns <see cref="null"/>.
        /// </summary>
        /// <param name="settings">The dictionary.</param>
        /// <param name="key">Key.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <returns>Either item associated with key, or null, if not present.</returns>
        public static object GetValueOrNullSafe(this Dictionary<string, object> settings, string key)
            => (settings ?? throw new ArgumentNullException($"{nameof(settings)} argument cannot be null."))
                .TryGetValue(key, out object item) ? item : null;
                  

    }
}
