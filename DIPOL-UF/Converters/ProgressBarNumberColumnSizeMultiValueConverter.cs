using System;
using System.Collections.Generic;
using System.Globalization;
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


namespace DIPOL_UF.Converters
{
    class ProgressBarNumberColumnSizeMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] is bool isIndeterminate)
                return new GridLength(0);
            else if (values[0] is TextBlock && values[1] is string)
            {
                var size = Helper.MeasureString((string)values[1], (TextBlock)values[0]);

                return new GridLength(size.Width + 20);
            }
            else
                return new GridLength(100);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
