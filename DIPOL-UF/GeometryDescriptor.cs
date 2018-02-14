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
        private readonly Func<double, double, GeometryDescriptor, bool> _isInRegionChecker;

        public GeometryDescriptor(Point center, Size size,
            List<Tuple<Point, Action<StreamGeometryContext, Point>>> path,
            double thickness,
            Func<double, double, GeometryDescriptor, bool> pixelChecker)
        {
            Center = new Point(center.X, center.Y);
            Size = new Size(size.Width + thickness, size.Height + thickness);
            HalfSize = new Size(Size.Width/2, Size.Height/2);
            PathDescription = path;
            Thickness = thickness;
            _isInRegionChecker = pixelChecker;
        }

        public List<(int X, int Y)> PixelsInsideGeometry(Point currentPos, 
            int pixelWidth, int pixelHeight, 
            double actualRegionWidth, double actualRegionHeight)
        {
            var relativePos = new Point(currentPos.X / actualRegionWidth, currentPos.Y / actualRegionHeight);
            var relativeHalfSize = new Size((HalfSize.Width + Thickness) / actualRegionWidth, (HalfSize.Height +Thickness)/ actualRegionHeight);

            var pixelXLims = (Min: Convert.ToInt32(Floor(Max(relativePos.X - relativeHalfSize.Width, 0) * pixelWidth)),
                              Max: Convert.ToInt32(Ceiling(Min(relativePos.X + relativeHalfSize.Width, 1) * pixelWidth)));

            var pixelYLims = (Min: Convert.ToInt32(Floor(Max(relativePos.Y - relativeHalfSize.Height, 0) * pixelHeight)),
                              Max: Convert.ToInt32(Ceiling(Min(relativePos.Y + relativeHalfSize.Height, 1) * pixelHeight)));

            var pixels = new List<(int X, int Y)>((pixelXLims.Max - pixelXLims.Min) * (pixelYLims.Max - pixelYLims.Min)/ 2);

            for (var pixIndX = pixelXLims.Min; pixIndX <=  pixelXLims.Max; pixIndX++)
                for (var pixIndY = pixelYLims.Min; pixIndY <= pixelYLims.Max; pixIndY++)
                    if(_isInRegionChecker(
                        1.0 * pixIndX / pixelWidth * actualRegionWidth - currentPos.X, 
                        1.0 * pixIndY / pixelHeight * actualRegionHeight - currentPos.Y, 
                        this))
                        pixels.Add((pixIndX, pixIndY));

            return pixels;
        }

    }   
}
