﻿//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017, Ilia Kosenkov, Tuorla Observatory, Finland

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

        public override string ToString()
        {
            return String.Format("{0}, {1}, {2}, {3}", X1, Y1, X2, Y2);
        }

        public static Rectangle Parse(string source)
        {
            var split = source.Trim().Split(',');

            if (split.Length != 4)
                throw new ArgumentException($"String {source} cannot be parsed into {typeof(Rectangle)}.");


            var coords = split.Select(s => int.Parse(s, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo)).ToArray();

            return new Rectangle(coords[0], coords[1], coords[2], coords[3]);
        }
    }

}
