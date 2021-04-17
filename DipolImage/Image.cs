using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using IK.ILSpanCasts;
using MathNet.Numerics;
using Microsoft.Toolkit.HighPerformance;

[assembly:InternalsVisibleTo("Tests")]
namespace DipolImage
{
    [DebuggerDisplay(@"\{Image ({Height} x {Width}) of type {UnderlyingType}\}")]
    [DataContract]
    public abstract class Image : IEquatable<Image>
    {
        // One row of standard i32 image is 4 * 512 = 2048 bytes
        private const int StackAllocByteLimit = 2048;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly TypeCode[] AllowedTypes =
         {
            TypeCode.Double,
            TypeCode.Single,
            TypeCode.UInt16,
            TypeCode.Int16,
            TypeCode.UInt32,
            TypeCode.Int32,
            TypeCode.Byte,
            TypeCode.SByte
        };

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Dictionary<TypeCode, Type> TypeCodeMap = new()
        {
            {TypeCode.Double, typeof(double)},
            {TypeCode.Single, typeof(float)},
            {TypeCode.UInt16, typeof(ushort)},
            {TypeCode.Int16, typeof(short)},
            {TypeCode.UInt32, typeof(uint)},
            {TypeCode.Int32, typeof(int)},
            {TypeCode.Byte, typeof(byte)},
            {TypeCode.SByte, typeof(sbyte)}
        };


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Dictionary<TypeCode, int> TypeSizes = new()
        {
            {TypeCode.Double, sizeof(double)},
            {TypeCode.Single, sizeof(float)},
            {TypeCode.UInt16, sizeof(ushort)},
            {TypeCode.Int16, sizeof(short)},
            {TypeCode.UInt32, sizeof(uint)},
            {TypeCode.Int32, sizeof(int)},
            {TypeCode.Byte, sizeof(byte)},
            {TypeCode.SByte, sizeof(sbyte)}
        };


        public static IReadOnlyCollection<TypeCode> AllowedPixelTypes
            => AllowedTypes;

        [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [field: DataMember]
        public TypeCode UnderlyingType { get; }

        [field: DataMember]
        public int Width
        {
            get;
        }
        [field: DataMember]
        public int Height
        {
            get;
        }

        public int ItemSizeInBytes { get; }

        public Type Type { get; }

        public abstract object this[int i, int j] { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Get<T>(int i, int j) where T : unmanaged =>
            ref TypedView<T>()[i * Width + j];

        protected Image(int width, int height, TypeCode type)
        {
            if(width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException($"Image size is incorrect [{width}, {height}].");
            }

            if (!Enum.IsDefined(typeof(TypeCode), type))
            {
                throw new ArgumentException($"Parameter type ({type}) is not defined in {typeof(TypeCode)}.");
            }

            if (!AllowedTypes.Contains(type))
            {
                throw new ArgumentException($"Specified type {type} is not allowed.");
            }

            Width = width;
            Height = height;
            UnderlyingType = type;
            ItemSizeInBytes = ResolveItemSize(type);
            Type = ResolveType(type);
        }
        
        [Obsolete("Use `" + nameof(ByteView) + "`.")]
        public abstract byte[] GetBytes();
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ByteView() => UnsafeAsBytes();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan2D<byte> ByteView2D() => UnsafeAsBytes2D();


        public double Max()
        {
            double max;

            switch (UnderlyingType)
            {
                case TypeCode.Byte:
                {
                    var localMax = byte.MinValue;
                    var arr = TypedView<byte>();
                    for (var i = 0; i < arr.Length; i++)
                    {
                        localMax = Ops.Max(arr[i], localMax);
                    }

                    max = localMax;
                    break;
                }
                case TypeCode.SByte:
                {
                    var localMax = sbyte.MinValue;
                    var arr = TypedView<sbyte>();
                    for (var i = 0; i < arr.Length; i++)
                    {
                        localMax = Ops.Max(arr[i], localMax);
                    }

                    max = localMax;
                    break;
                }
                case TypeCode.UInt16:
                {
                    var localMax = ushort.MinValue;
                    var arr = TypedView<ushort>();
                    for (var i = 0; i < arr.Length; i++)
                    {
                        localMax = Ops.Max(arr[i], localMax);
                    }

                    max = localMax;
                    break;
                }
                case TypeCode.Int16:
                {
                    var localMax = short.MinValue;
                    var arr = TypedView<short>();
                    for (var i = 0; i < arr.Length; i++)
                    {
                        localMax = Ops.Max(arr[i], localMax);
                    }

                    max = localMax;
                    break;
                }
                case TypeCode.UInt32:
                {
                    var localMax = uint.MinValue;
                    var arr = TypedView<uint>();
                    for (var i = 0; i < arr.Length; i++)
                    {
                        localMax = Ops.Max(arr[i], localMax);
                    }

                    max = localMax;
                    break;
                }
                case TypeCode.Int32:
                {
                    var localMax = int.MinValue;
                    var arr = TypedView<int>();
                    for (var i = 0; i < arr.Length; i++)
                    {
                        localMax = Ops.Max(arr[i], localMax);
                    }

                    max = localMax;
                    break;
                }
                case TypeCode.Single:
                {
                    var arr = TypedView<float>();
                    var localMax = arr[0];
                    for (var i = 0; i < arr.Length; i++)
                    {
                        localMax = Ops.Max(arr[i], localMax);
                    }

                    max = localMax;
                    break;
                }
                default:
                {
                    var arr = TypedView<double>();
                    var localMax = arr[0];
                    for (var i = 0; i < arr.Length; i++)
                    {
                        localMax = Ops.Max(arr[i], localMax);
                    }

                    max = localMax;
                    break;
                }

            }

            return max;
        }

        public double Min()
        {
            double min;

            switch (UnderlyingType)
            {
                case TypeCode.Byte:
                {
                    var localMin = byte.MaxValue;
                    var arr = TypedView<byte>();
                    for (var i = 0; i < arr.Length; i++)
                    {
                        localMin = Ops.Min(arr[i], localMin);
                    }

                    min = localMin;
                    break;
                }
                case TypeCode.SByte:
                {
                    var localMin = sbyte.MaxValue;
                    var arr = TypedView<sbyte>();
                    for (var i = 0; i < arr.Length; i++)
                    {
                        localMin = Ops.Min(arr[i], localMin);
                    }

                    min = localMin;
                    break;
                }
                case TypeCode.UInt16:
                    {
                        var localMin = ushort.MaxValue;
                        var arr = TypedView<ushort>();
                        for (var i = 0; i < arr.Length; i++)
                        {
                            localMin = Ops.Min(arr[i], localMin);
                        }

                        min = localMin;
                        break;
                    }
                case TypeCode.Int16:
                    {
                        var localMin = short.MaxValue;
                        var arr = TypedView<short>();
                        for (var i = 0; i < arr.Length; i++)
                        {
                            localMin = Ops.Min(arr[i], localMin);
                        }

                        min = localMin;
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        var localMin = uint.MaxValue;
                        var arr = TypedView<uint>();
                        for (var i = 0; i < arr.Length; i++)
                        {
                            localMin = Ops.Min(arr[i], localMin);
                        }

                        min = localMin;
                        break;
                    }
                case TypeCode.Int32:
                    {
                        var localMin = int.MaxValue;
                        var arr = TypedView<int>();
                        for (var i = 0; i < arr.Length; i++)
                        {
                            localMin = Ops.Min(arr[i], localMin);
                        }

                        min = localMin;
                        break;
                    }
                case TypeCode.Single:
                {
                    var arr = TypedView<float>();
                    var localMin = arr[0];
                    for (var i = 1; i < arr.Length; i++)
                    {
                        localMin = Ops.Min(arr[i], localMin);
                    }

                    min = localMin;
                    break;
                }
                default:
                {
                    var arr = TypedView<double>();
                    var localMin = arr[0];
                    for (var i = 1; i < arr.Length; i++)
                    {
                        localMin = Ops.Min(arr[i], localMin);
                    }

                    min = localMin;
                    break;
                }
            }



            return min;
        }

        public abstract Image Copy();

        public void Clamp(double low, double high)
        {
            if (high <= low)
                throw new ArgumentException(@"[high] should be greater than [low]");

            switch (UnderlyingType)
            {
                case TypeCode.Byte:
                {
                    var locLow = (byte) (Math.Floor(low));
                    var locHigh = (byte) (Math.Ceiling(high));
                    var arr = UnsafeAsSpan<byte>();

                    for (var i = 0; i < arr.Length; i++)
                    {
                        arr[i] = Ops.Clamp(arr[i], locLow, locHigh);
                    }

                    break;
                }
                case TypeCode.SByte:
                {
                    var locLow = (sbyte) (Math.Floor(low));
                    var locHigh = (sbyte) (Math.Ceiling(high));
                    var arr = UnsafeAsSpan<sbyte>();

                    for (var i = 0; i < arr.Length; i++)
                    {
                        arr[i] = Ops.Clamp(arr[i], locLow, locHigh);
                    }

                    break;
                }

                case TypeCode.UInt16:
                {
                    var locLow = (ushort) (Math.Floor(low));
                    var locHigh = (ushort) (Math.Ceiling(high));
                    var arr = UnsafeAsSpan<ushort>();

                    for (var i = 0; i < arr.Length; i++)
                    {
                        arr[i] = Ops.Clamp(arr[i], locLow, locHigh);
                    }
                    break;
                }
                case TypeCode.Int16:
                {
                    var locLow = (short) (Math.Floor(low));
                    var locHigh = (short) (Math.Ceiling(high));
                    var arr = UnsafeAsSpan<short>();

                    for (var i = 0; i < arr.Length; i++)
                    {
                        arr[i] = Ops.Clamp(arr[i], locLow, locHigh);
                    }
                    break;
                }
                case TypeCode.UInt32:
                {
                    var locLow = (uint) (Math.Floor(low));
                    var locHigh = (uint) (Math.Ceiling(high));
                    var arr = UnsafeAsSpan<uint>();

                    for (var i = 0; i < arr.Length; i++)
                    {
                        arr[i] = Ops.Clamp(arr[i], locLow, locHigh);
                    }
                    break;
                }
                case TypeCode.Int32:
                {
                    var locLow = (int) (Math.Floor(low));
                    var locHigh = (int) (Math.Ceiling(high));
                    var arr = UnsafeAsSpan<int>();

                    for (var i = 0; i < arr.Length; i++)
                    {
                        arr[i] = Ops.Clamp(arr[i], locLow, locHigh);
                    }
                    break;
                }
                case TypeCode.Single:
                {
                    var locLow = (float) (low);
                    var locHigh = (float) (high);
                    var arr = UnsafeAsSpan<float>();

                    for (var i = 0; i < arr.Length; i++)
                    {
                        arr[i] = Ops.Clamp(arr[i], locLow, locHigh);
                    }
                    break;
                }
                default:
                {
                    var arr = UnsafeAsSpan<double>();

                    for (var i = 0; i < arr.Length; i++)
                    {
                        arr[i] = Ops.Clamp(arr[i], low, high);
                    }
                    break;
                }

            }
        }

        public void Scale(double gMin, double gMax)
        {
            if (gMax <= gMin)
                throw new ArgumentException(@"[high] should be greater than [low]");

            var min = Min();
            var max = Max();

            var factor = 1.0 * (gMax - gMin) / (max - min);


            switch (UnderlyingType)
            {
                case TypeCode.Byte:
                {
                    var arr = UnsafeAsSpan<byte>();

                    if (min.AlmostEqual(max))
                    {
                        var val = (byte) (0.5 * (gMin + gMax));
                        arr.Fill(val);
                    }
                    else
                    {
                        for (var i = 0; i < arr.Length; i++)
                        {
                            arr[i] = (byte) (gMin + factor * (arr[i] - min));
                        }
                    }

                    break;
                }
                
                case TypeCode.SByte:
                {
                    var arr = UnsafeAsSpan<sbyte>();

                    if (min.AlmostEqual(max))
                    {
                        var val = (sbyte) (0.5 * (gMin + gMax));
                        arr.Fill(val);
                    }
                    else
                    {
                        for (var i = 0; i < arr.Length; i++)
                        {
                            arr[i] = (sbyte) (gMin + factor * (arr[i] - min));
                        }
                    }

                    break;
                }
                case TypeCode.UInt16:
                {
                    var arr = UnsafeAsSpan<ushort>();

                    if (min.AlmostEqual(max))
                    {
                        var val = (ushort) (0.5 * (gMin + gMax));
                        arr.Fill(val);
                    }
                    else
                    {
                        for (var i = 0; i < arr.Length; i++)
                        {
                            arr[i] = (ushort) (gMin + factor * (arr[i] - min));
                        }
                    }


                    break;
                }
                case TypeCode.Int16:
                {
                    var arr = UnsafeAsSpan<short>();

                    if (min.AlmostEqual(max))
                    {
                        var val = (short) (0.5 * (gMin + gMax));
                        arr.Fill(val);
                    }
                    else
                    {
                        for (var i = 0; i < arr.Length; i++)
                        {
                            arr[i] = (short) (gMin + factor * (arr[i] - min));
                        }
                    }

                    break;
                }
                case TypeCode.UInt32:
                {
                    var arr = UnsafeAsSpan<uint>();

                    if (min.AlmostEqual(max))
                    {
                        var val = (uint) (0.5 * (gMin + gMax));
                        arr.Fill(val);
                    }
                    else
                    {
                        for (var i = 0; i < arr.Length; i++)
                        {
                            arr[i] = (uint) (gMin + factor * (arr[i] - min));
                        }
                    }

                    break;
                }
                case TypeCode.Int32:
                {
                    var arr = UnsafeAsSpan<int>();

                    if (min.AlmostEqual(max))
                    {
                        var val = (int) (0.5 * (gMin + gMax));
                        arr.Fill(val);
                    }
                    else
                    {
                        for (var i = 0; i < arr.Length; i++)
                        {
                            arr[i] = (int)(gMin + factor * (arr[i] - min));
                        }
                    }

                    break;
                }
                case TypeCode.Single:
                {
                    var arr = UnsafeAsSpan<float>();

                    if (min.AlmostEqual(max))
                    {
                        var val = (float) (0.5 * (gMin + gMax));
                        arr.Fill(val);
                    }
                    else
                    {
                        for (var i = 0; i < arr.Length; i++)
                        {
                            arr[i] = (float) (gMin + factor * (arr[i] - min));
                        }
                    }

                    break;
                }
                default:
                {
                    var arr = UnsafeAsSpan<double>();

                    if (min.AlmostEqual(max))
                    {
                        var val = 0.5 * (gMin + gMax);
                        arr.Fill(val);
                    }

                    else
                    {
                        for (var i = 0; i < arr.Length; i++)
                        {
                            arr[i] = gMin + factor * (arr[i] - min);
                        }
                    }

                    break;
                }
            }
        }

        public double Percentile(double lvl)
        {
            if (lvl < 0 | lvl > 1.0)
                throw new ArgumentOutOfRangeException($"{nameof(lvl)} parameter is out of range ({lvl} should be in [0, 1]).");

            if (lvl.AlmostEqual(0.0))
            {
                return Min();
            }

            if (lvl.AlmostEqual(1.0))
            {
                return Max();
            }
            
            var length = (int)Math.Ceiling(lvl * Width * Height);
            return UnderlyingType switch
            {
                TypeCode.Byte => Percentile<byte>(length),
                TypeCode.SByte => Percentile<sbyte>(length),
                TypeCode.UInt16 => Percentile<ushort>(length),
                TypeCode.Int16 => Percentile<short>(length),
                TypeCode.UInt32 => Percentile<uint>(length),
                TypeCode.Int32 => Percentile<int>(length),
                TypeCode.Single => Percentile<float>(length),
                _ => Percentile<double>(length)
            };
        }

        public AllocatedImage CastTo<TS, TD>(Func<TS, TD> cast)
            where TS : unmanaged
            where TD : unmanaged
        {
            const int unrollBy = 8;
            if (typeof(TS) != Type)
            {
                throw new TypeAccessException($"Source type {typeof(TS)} differs from underlying type with code {UnderlyingType}.");
            }

            var (width, height) = (Width, Height);
            
            var buffer = new TD[width * height];
            var view = TypedView<TS>();

            var i = 0;
            for (; i < width * height; i += unrollBy)
            {
                // Unroll by 8
                buffer[i] = cast(view[i]);
                buffer[i + 1] = cast(view[i + 1]);
                buffer[i + 2] = cast(view[i + 2]);
                buffer[i + 3] = cast(view[i + 3]);
                buffer[i + 4] = cast(view[i + 4]);
                buffer[i + 5] = cast(view[i + 5]);
                buffer[i + 6] = cast(view[i + 6]);
                buffer[i + 7] = cast(view[i + 7]);
            }

            i -= unrollBy;
            if (i < width * height)
            {
                for (; i < width * height; i++)
                {
                    buffer[i] = cast(view[i]);
                }
            }

            return new AllocatedImage(buffer, width, height, false);
        }

        public ReadOnlySpan<T> TypedView<T>() where T : unmanaged =>
            typeof(T) == Type
                ? UnsafeAsSpan<T>()
                : throw new ArrayTypeMismatchException();

        public ReadOnlySpan2D<T> TypedView2D<T>() where T : unmanaged =>
            typeof(T) == Type
                ? UnsafeAsSpan2D<T>()
                : throw new ArrayTypeMismatchException();

        public Image Transpose() =>
            GetConstructor()
                .CreateAndFill(Height, Width, UnderlyingType, Transpose, this);
        
        public Image Reflect(ReflectionDirection direction)
        {
            return direction switch
            {
                ReflectionDirection.NoReflection => Copy(),
                ReflectionDirection.Vertical => 
                    GetConstructor().
                        CreateAndFill(Width, Height, UnderlyingType, ReflectVertically, this),
                ReflectionDirection.Horizontal =>
                    GetConstructor()
                        .CreateAndFill(Width, Height, UnderlyingType, ReflectHorizontally, this),
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }
        
        public Image Rotate(RotateBy rotateBy, RotationDirection direction) =>
            (rotateBy, direction) switch
            {
                (RotateBy.Deg0, _) => Copy(),
                (RotateBy.Deg90, RotationDirection.Left) => 
                    GetConstructor().
                        CreateAndFill(Height, Width, UnderlyingType, RotateBy90CounterClock, this),
                (RotateBy.Deg180, RotationDirection.Left) =>
                    GetConstructor()
                        .CreateAndFill(Width, Height, UnderlyingType, RotateBy180CounterClock, this),
                (RotateBy.Deg270, RotationDirection.Left) =>
                    GetConstructor()
                        .CreateAndFill(Height, Width, UnderlyingType, RotateBy270CounterClock, this),
        
                (RotateBy.Deg90, RotationDirection.Right) =>
                    GetConstructor()
                        .CreateAndFill(Height, Width, UnderlyingType, RotateBy270CounterClock, this),
                (RotateBy.Deg180, RotationDirection.Right) =>
                    GetConstructor()
                        .CreateAndFill(Width, Height, UnderlyingType, RotateBy180CounterClock, this),
                (RotateBy.Deg270, RotationDirection.Right) =>
                    GetConstructor()
                        .CreateAndFill(Height, Width, UnderlyingType, RotateBy90CounterClock, this),
                _ => throw new InvalidOperationException("Image transformation not supported.")
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Image? other) => Equals(other, FloatingPointComparisonType.Loose);

        public bool Equals(Image? other, FloatingPointComparisonType compType)
        {
            if (UnderlyingType != other?.UnderlyingType ||
                Width != other.Width ||
                Height != other.Height)
                return false;

            switch (UnderlyingType)
            {

                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return ByteView().SequenceEqual(other.ByteView());
                case TypeCode.Single:
                {
                    if (compType != FloatingPointComparisonType.Loose)
                    {
                        return ByteView().SequenceEqual(other.ByteView());
                    }

                    var thisArr = TypedView<float>();
                    var otherArr = other.TypedView<float>();
                    for (var i = 0; i < Width * Height; i++)
                    {
                        if (Ops.Equal(thisArr[i], otherArr[i]))
                        {
                            continue;
                        }

                        return false;
                    }

                    return true;

                }
                default:
                {
                    if (compType != FloatingPointComparisonType.Loose)
                    {
                        return ByteView().SequenceEqual(other.ByteView());
                    }

                    var thisArr = TypedView<double>();
                    var otherArr = other.TypedView<double>();
                    for (var i = 0; i < Width * Height; i++)
                    {
                        if (Ops.Equal(thisArr[i], otherArr[i]))
                        {
                            continue;
                        }

                        return false;
                    }

                    return true;
                }
              
            }
        }

        public override bool Equals(object? obj) =>
            obj is Image im && im.Equals(this);

        public override int GetHashCode()
        {
            ReadOnlySpan<byte> view = ByteView();
            var hash = new HashCode();
            for (var i = 0; i < view.Length; i++)
            {
                hash.Add(view[i]);
            }

            return hash.ToHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Span<byte> UnsafeAsBytes() =>
            UnderlyingType switch
            {
                TypeCode.Double => MemoryMarshal.AsBytes(UnsafeAsSpan<double>()),
                TypeCode.Single => MemoryMarshal.AsBytes(UnsafeAsSpan<float>()),
                TypeCode.UInt16 => MemoryMarshal.AsBytes(UnsafeAsSpan<ushort>()),
                TypeCode.Int16 => MemoryMarshal.AsBytes(UnsafeAsSpan<short>()),
                TypeCode.UInt32 => MemoryMarshal.AsBytes(UnsafeAsSpan<uint>()),
                TypeCode.Int32 => MemoryMarshal.AsBytes(UnsafeAsSpan<int>()),
                TypeCode.SByte => MemoryMarshal.AsBytes(UnsafeAsSpan<sbyte>()),
                TypeCode.Byte => UnsafeAsSpan<byte>(),
                _ => default // This is unreachable
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Span2D<byte> UnsafeAsBytes2D() => UnsafeAsBytes().ToSpan2D(Width * ItemSizeInBytes, Height);
           
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract Span<T> UnsafeAsSpan<T>() where T : unmanaged;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract Span2D<T> UnsafeAsSpan2D<T>() where T : unmanaged;

        private protected abstract IImageConstructor GetConstructor();

        private static void ReflectVertically(Span<byte> dataView, Image image)
        {
            var size = TypeSizes[image.UnderlyingType];
            var width = image.Width;
            var height = image.Height;
            var rowWidth = width * size;

            image.ByteView().CopyTo(dataView);

            byte[]? arrayBuff = null;
            try
            {
                var buffer = rowWidth <= StackAllocByteLimit
                    ? stackalloc byte[rowWidth]
                    : (arrayBuff = ArrayPool<byte>.Shared.Rent(rowWidth)).AsSpan(0, rowWidth);

                // 1 2 3      7 8 9
                // 4 5 6  ->  4 5 6
                // 7 8 9      1 2 3
                for (var rowId = 0; rowId < height / 2; rowId++)
                {
                    var top = dataView.Slice(rowId * rowWidth, rowWidth);
                    var bottom = dataView.Slice((height - rowId - 1) * rowWidth, rowWidth);
                    top.CopyTo(buffer);
                    bottom.CopyTo(top);
                    buffer.CopyTo(bottom);
                }
            }
            finally
            {
                if (arrayBuff is { })
                {
                    ArrayPool<byte>.Shared.Return(arrayBuff);
                }
            }
        }

        private static void ReflectHorizontally(Span<byte> dataView, Image image)
        {
            var size = TypeSizes[image.UnderlyingType];
            var width = image.Width;
            var height = image.Height;
            var rowWidth = width * size;

            image.ByteView().CopyTo(dataView);

            Span<byte> buffer = stackalloc byte[size];
            // 1 2 3 4 5  \   5 4 3 2 1
            // 6 7 8 9 0  /   0 9 8 7 6
            for (var rowId = 0; rowId < height; rowId++)
            {
                var rowData = dataView.Slice(rowId * rowWidth, rowWidth);
                // 1 2 3 4 5 6 -> 6 5 4 3 2 1
                for (var colId = 0; colId < width / 2; colId++)
                {
                    var left = rowData.Slice(colId * size, size);
                    var right = rowData.Slice((width - colId - 1) * size, size);
                    left.CopyTo(buffer);
                    right.CopyTo(left);
                    buffer.CopyTo(right);
                }
            }
        }

        private static void RotateBy90CounterClock(Span<byte> dataView, Image image)
        {
            var size = TypeSizes[image.UnderlyingType];
            var width = image.Width;
            var height = image.Height;

            var sourceView = image.ByteView();
            
            // 1 2 3     3 6
            // 4 5 6 ->  2 5
            //           1 4

            for (var rowId = 0; rowId < height; rowId++)
            {
                for (var colId = 0; colId < width; colId++)
                {
                    var src = sourceView.Slice((rowId * width + colId) * size, size);
                    var dest = dataView.Slice(((width - colId - 1) * height + rowId) * size, size);
                    src.CopyTo(dest);
                }
            }
        }

        private static void RotateBy180CounterClock(Span<byte> dataView, Image image)
        {
            var size = TypeSizes[image.UnderlyingType];
            var width = image.Width;
            var height = image.Height;
            var rowWidth = width * size;
            
            image.ByteView().CopyTo(dataView);

            byte[]? arrayBuff = null;
            try
            {
                Span<byte> buffer = rowWidth <= StackAllocByteLimit
                    ? stackalloc byte[rowWidth]
                    : (arrayBuff = ArrayPool<byte>.Shared.Rent(rowWidth)).AsSpan(0, rowWidth);

                // 1 2 3 \  6 5 4
                // 4 5 6 /  3 2 1
                for (var rowId = 0; rowId < height / 2; rowId++)
                {
                    var top = dataView.Slice(rowId * rowWidth, rowWidth);
                    var bottom = dataView.Slice((height - rowId - 1) * rowWidth, rowWidth);
                    top.CopyTo(buffer);
                    bottom.CopyTo(top);
                    buffer.CopyTo(bottom);
                    
                    Reverse(top, size);
                    Reverse(bottom, size);
                }

            }
            finally
            {
                if (arrayBuff is { })
                {
                    ArrayPool<byte>.Shared.Return(arrayBuff);
                }
            }
        }

        private static void RotateBy270CounterClock(Span<byte> dataView, Image image)
        {
            var size = TypeSizes[image.UnderlyingType];
            var width = image.Width;
            var height = image.Height;

            var sourceView = image.ByteView();

            // 1 2 3     4 1
            // 4 5 6 ->  5 2
            //           6 3

            for (var rowId = 0; rowId < height; rowId++)
            {
                for (var colId = 0; colId < width; colId++)
                {
                    var src = sourceView.Slice((rowId * width + colId) * size, size);
                    var dest = dataView.Slice((colId * height + (height - rowId - 1)) * size, size);
                    src.CopyTo(dest);
                }
            }
        }

        private static void Transpose(Span<byte> dataView, Image image)
        {
            var width = image.Width;
            var height = image.Height;
            var size = image.ItemSizeInBytes;
            ReadOnlySpan<byte> sourceView = image.ByteView();

            for (var rowId = 0; rowId < height; rowId++)
            {
                for (var colId = 0; colId < width; colId++)
                {
                    ReadOnlySpan<byte> src = sourceView.Slice((rowId * width  + colId) * size, size);
                    Span<byte> dst = dataView.Slice((colId * height + rowId) * size, size);
                    src.CopyTo(dst);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Type ResolveType(TypeCode code) => TypeCodeMap[code];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ResolveItemSize(TypeCode code) => TypeSizes[code];

        private T Percentile<T>(int length) where T : unmanaged
        {
            var view = TypedView<T>();
            if (length >= view.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            T[]? arrayBuffer = null;
            try
            {
                arrayBuffer = ArrayPool<T>.Shared.Rent(view.Length);
                var arrayView = arrayBuffer.AsSpan(0, view.Length);
                view.CopyTo(arrayView);
                Array.Sort(arrayBuffer, 0, view.Length);

                return arrayBuffer[length];
            }
            finally
            {
                ArrayPool<T>.Shared.Return(arrayBuffer);
            }
        }

        private static void Reverse(Span<byte> span, int itemSizeInBytes)
        {
            // Assuming `span` contains exactly `n` elements of size `itemSizeInBytes`
            var n = span.Length / itemSizeInBytes;

            Span<byte> buff = stackalloc byte[itemSizeInBytes];
            
            for (var i = 0; i < n / 2; i++)
            {
                var left = span.Slice(i * itemSizeInBytes, itemSizeInBytes);
                var right = span.Slice((n - i - 1) * itemSizeInBytes, itemSizeInBytes);
                right.CopyTo(buff);
                left.CopyTo(right);
                buff.CopyTo(left);
            }
        }
    }
}
