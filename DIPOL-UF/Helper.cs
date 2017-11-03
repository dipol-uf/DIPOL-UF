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
using System.Windows.Shapes;

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

            return parent;
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
    }
}
