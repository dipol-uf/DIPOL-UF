using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Data;
using System.Windows.Markup;

namespace ImageDisplayLib.Converters
{
    public class Slider2_CanvasWidthTrackWidthThumbConverter : MarkupExtension, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var dblVals = values.Cast<double>().ToArray();

            double shift = 0.5 * (dblVals[0] - dblVals[1]);

            double linear = (dblVals[2] - dblVals[3])/(dblVals[4] - dblVals[3]);

            return shift + linear * dblVals[1] - dblVals[5]/2;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
            => this;
    }
}
