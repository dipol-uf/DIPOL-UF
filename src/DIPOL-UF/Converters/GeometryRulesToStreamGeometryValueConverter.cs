using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using System.Globalization;
using System.Windows.Media;

namespace DIPOL_UF.Converters
{
    [ValueConversion(typeof(object), typeof(StreamGeometry))]
    public class GeometryRulesToStreamGeometryValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<Tuple<Point, Action<StreamGeometryContext, Point>>> list && 
                !DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                var geom = new StreamGeometry();
                using (var cont = geom.Open())
                {
                    cont.BeginFigure(list[0].Item1, true, true);
                    
                    for (var i = 1; i < list.Count; i++)
                        list[i].Item2(cont, list[i].Item1);
                }
                geom.Freeze();
                return geom.GetFlattenedPathGeometry();
            }

            // Default & displayed in designer
            return new StreamGeometry().GetFlattenedPathGeometry();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
