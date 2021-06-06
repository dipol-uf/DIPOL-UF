#nullable enable
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DIPOL_UF.Enums;

namespace DIPOL_UF
{
    internal static class ImageProvider
    {
        public static Visual ProvideLinearPolSymbol(int width, int height)
        {
            var size = Math.Sqrt(width * width + height * height);
            var thickness = size / 50;
            var delta = size / 20;
            var offset = thickness / 3;
            
            var redPen = new Pen(Brushes.Firebrick, thickness);
            var bluePen = new Pen(Brushes.CornflowerBlue, thickness);
            
            var visual = new DrawingVisual();
            using var ctx = visual.RenderOpen();
            
            ctx.DrawLine(redPen, new Point(width / 2.0, 0), new Point(width / 2.0, height));
            
            ctx.DrawLine(redPen, new Point(width / 2.0, offset), new Point(width / 2.0 + delta, delta + offset));
            ctx.DrawLine(redPen, new Point(width / 2.0, offset), new Point(width / 2.0 - delta, delta + offset));
           
            ctx.DrawLine(redPen, new Point(width / 2.0, height - offset), new Point(width / 2.0 + delta, height - delta - offset));
            ctx.DrawLine(redPen, new Point(width / 2.0, height - offset), new Point(width / 2.0 - delta, height - delta - offset));
            
            
            ctx.DrawLine(bluePen, new Point(0, height / 2.0), new Point(width, height / 2.0));
            
            ctx.DrawLine(bluePen, new Point(offset, height / 2.0), new Point(offset + delta, height / 2.0 - delta));
            ctx.DrawLine(bluePen, new Point(offset, height / 2.0), new Point(offset + delta, height / 2.0 + delta));

            ctx.DrawLine(bluePen, new Point(width - offset, height / 2.0), new Point(width - offset - delta, height / 2.0 - delta));
            ctx.DrawLine(bluePen, new Point(width - offset, height / 2.0), new Point(width - offset - delta, height / 2.0 + delta));

            return visual;
        }

        public static Visual ProvideCircularPolSymbol(int width, int height)
        {
            var size = Math.Sqrt(width * width + height * height);
            var thickness = size / 50;
            var delta = size / 20;
            var offset = thickness;

            var brush = new SolidColorBrush(new Color {A = 0, R = 255, G = 255, B = 0});
            var pen = new Pen(Brushes.DarkGreen, thickness);
            var visual = new DrawingVisual();
            using var ctx = visual.RenderOpen();
            
            ctx.DrawEllipse(brush, pen, new Point(width / 2.0,  height / 2.0), width / 2.0 - offset - delta, height / 2.0 - offset - delta);
         
            ctx.DrawLine(pen, new Point(width / 2.0 - delta / 2.0, offset + delta), new Point(width / 2.0 + delta / 2.0, offset));
            ctx.DrawLine(pen, new Point(width / 2.0 - delta / 2.0, offset + delta), new Point(width / 2.0 + delta / 2.0, offset + 2 * delta));
            
            ctx.DrawLine(pen, new Point(width / 2.0 + delta / 2.0, height - offset - delta), new Point(width / 2.0 - delta / 2.0, height - offset));
            ctx.DrawLine(pen, new Point(width / 2.0 + delta / 2.0, height - offset - delta), new Point(width / 2.0 - delta / 2.0, height - offset - 2 * delta));
            
            return visual;
        }

        public static void UpdateBitmap(RenderTargetBitmap bitmap, CycleType? type)
        {
            bitmap.Clear();
            switch (type)
            {
                case CycleType.LinearPolarimetry:
                    bitmap.Render(ProvideLinearPolSymbol(bitmap.PixelWidth, bitmap.PixelHeight));
                    return;
                case CycleType.CircularPolarimetry:
                    bitmap.Render(ProvideCircularPolSymbol(bitmap.PixelWidth, bitmap.PixelHeight));
                    return;
                default:
                    return;
            }
        }
    }
}