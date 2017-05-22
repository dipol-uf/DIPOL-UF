using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.DataStructures
{
    public struct Point2D
    {
        public int X
        {
            get;
            private set;
        }

        public int Y
        {
            get;
            private set;
        }

        public Point2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Point2D operator + (Point2D a, Point2D b)
        {
            return new Point2D(a.X + b.X, a.Y + b.Y);

        }

        public static Point2D operator -(Point2D a)
        {
            return new Point2D(-a.X, -a.Y);
        }

        public static Point2D operator -(Point2D a, Point2D b)
        {
            return a + (-b);
        }

        public static Point2D operator +(Point2D a, Size b)
        {
            return new Point2D(a.X + b.Horizontal, a.Y + b.Vertical);
        }

        public static Point2D operator -(Point2D a, Size b)
        {
            return new Point2D(a.X - b.Horizontal, a.Y - b.Vertical);
        }

        public override string ToString()
        {
            return String.Format("({0}, {1})", X, Y);
        }
    }
}
