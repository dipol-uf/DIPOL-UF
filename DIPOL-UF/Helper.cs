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
    }
}
