using System;
using System.Linq;
using System.Runtime.Serialization;

namespace ANDOR_CS.DataStructures
{
    [DataContract]
    public struct Rectangle
    {
        public class RectangleUpdater
        {
            public int X1 { get; set; }
            public int Y1 { get; set; }
            public int X2 { get; set; }
            public int Y2 { get; set; }
        }

        [IgnoreDataMember]
        public int X1 => Start.X;
        [IgnoreDataMember]
        public int X2 => End.X;
        [IgnoreDataMember]
        public int Y1 => Start.Y;
        [IgnoreDataMember]
        public int Y2 => End.Y;

        [DataMember(IsRequired = true)]
        public Point2D Start
        {
            get;
            set;
        }

        [DataMember(IsRequired = true)]
        public Point2D End
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public Size Size => new Size(X2 - X1 + 1, Y2 - Y1 + 1);
        [IgnoreDataMember]
        public int Width => X2 - X1 + 1;
        [IgnoreDataMember]
        public int Height => Y2 - Y1 + 1;

        public Rectangle(Point2D start, Point2D end)
        {
            if (start.X > end.X || start.Y > end.Y)
                throw new ArgumentException($"{nameof(start)} should point to lower left corner, {nameof(end)} - to upper right. (start: {start} and end: {end})");

            Start = start;
            End = end;
        }

        public Rectangle(int x1, int y1, int x2, int y2)
        {
            if (x2 < x1)
                throw new ArgumentOutOfRangeException(nameof(x2),
                    $"{nameof(x2)} should be greater than or equal to {nameof(x1)} ({x1} <= {x2})");
            if (y2 < y1)
                throw new ArgumentOutOfRangeException(nameof(y2),
                    $"{nameof(y2)} should be greater than or equal to {nameof(y1)} ({y1} <= {y2})");

            Start = new Point2D(x1, y1);
            End = new Point2D(x2, y2);
        }

        public Rectangle(Point2D start, int width, int height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width),
                    $"{nameof(width)} should be greater than or equal to {0} ({width} >= {0})");

            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height),
                    $"{nameof(height)} should be greater than or equal to {0} ({height} >= {0})");

            Start = start;
            End = start + new Size(width, height);
        }

        public Rectangle(Point2D start, Size size)
        {
            Start = start;
            End = start - new Point2D(1, 1) +  size;
        }

        public Rectangle((int X1, int Y1, int X2, int Y2) param)
            : this(param.X1, param.Y1, param.X2, param.Y2)
        {
        }

        public Rectangle CopyWithModifications(Action<RectangleUpdater> context)
        {
            var updater = new RectangleUpdater()
            {
                X1 = X1,
                Y1 = Y1,
                X2 = X2,
                Y2 = Y2
            };

            context(updater);

            return new Rectangle(updater.X1, updater.Y1, updater.X2, updater.Y2);
        }

        public void Deconstruct(out int x1, out int y1, out int x2, out int y2)
        {
            x1 = X1;
            x2 = X2;
            y1 = Y1;
            y2 = Y2;
        }

        public override string ToString()
        {
            return $"{X1}, {Y1}, {X2}, {Y2}";
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
