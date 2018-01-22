//    This file is part of Dipol-3 Camera Manager.

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
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace ANDOR_CS.DataStructures
{
    [DataContract]
    public struct Point2D
    {
        [DataMember(IsRequired = true)s]
        public int X
        {
            get;
            private set;
        }

        [DataMember(IsRequired = true)]
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
