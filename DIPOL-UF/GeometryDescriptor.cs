using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using static System.Math;

namespace DIPOL_UF
{
    public class GeometryDescriptor
    {
        public Point Center { get; }
        public List<Tuple<Point, Action<StreamGeometryContext, Point>>> PathDescription { get; }
        public Size Size { get; }
        public Size HalfSize { get; }
        public double Thickness { get; }
        public Func<int, int, Point, Size, double, bool> IsInsideChecker { get; }
        public GeometryDescriptor(Point center, Size size,
            List<Tuple<Point, Action<StreamGeometryContext, Point>>> path,
            double thickness,
            Func<int, int, Point, Size, double, bool> isInsideChecker = null)
        {
            Center = new Point(center.X, center.Y);
            Size = new Size(size.Width + thickness, size.Height + thickness);
            HalfSize = new Size(Size.Width/2, Size.Height/2);
            PathDescription = path;
            Thickness = thickness;
            IsInsideChecker = isInsideChecker;
        }

    }   
}
