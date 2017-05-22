using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.DataStructures
{
    public struct Rectangle
    {
        private Point2D start;
        private Point2D end;

        public int X1 => start.X;
        public int X2 => end.X;
        public int Y1 => start.Y;
        public int Y2 => end.Y;

        public Point2D Start => start;
        public Point2D End => end;

        public Size Size => new Size(X2 - X1 + 1, Y2 - Y1 + 1);
        public int Width => X2 - X1 + 1;
        public int Height => Y2 - Y1 + 1;

        public Rectangle(Point2D start, Point2D end)
        {
            if (start.X > end.X || start.Y > end.Y)
                throw new ArgumentOutOfRangeException($"{nameof(start)} should point to lower left corner, {nameof(end)} - to upper right. (start: {start} and end: {end})");

            this.start = start;
            this.end = end;
        }

        public Rectangle(int x1, int y1, int x2, int y2)
        {
            if (x2 < x1)
                throw new ArgumentOutOfRangeException($"{nameof(x2)} should be greater than or equal to {nameof(x1)} ({x1} <= {x2})");
            if (y2 < y1)
                throw new ArgumentOutOfRangeException($"{nameof(y2)} should be greater than or equal to {nameof(y1)} ({y1} <= {y2})");

            start = new Point2D(x1, y1);
            end = new Point2D(x2, y2);
        }

        public Rectangle(Point2D start, int width, int height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException($"{nameof(width)} should be greater than or equal to {0} ({width} >= {0})");

            if (height < 0)
                throw new ArgumentOutOfRangeException($"{nameof(height)} should be greater than or equal to {0} ({height} >= {0})");

            this.start = start;
            end = start + new Size(width, height);
        }

        public Rectangle(Point2D start, Size size)
        {
            this.start = start;
            end = start - new Point2D(1, 1) +  size;
        }
    }

}
