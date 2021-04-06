using System;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly:InternalsVisibleTo("Tests")]
namespace DipolImage
{
    [DebuggerDisplay(@"\{Image ({Height} x {Width}) of type {UnderlyingType}\}")]
    [DataContract]
    public class Image : ImageBase, IEquatable<Image>
    {
        private static ImageCtor Ctor { get; } = new ImageCtor();
        
        [DataMember]
        private readonly Array _baseArray;

        public override object this[int i, int j] => _baseArray.GetValue(i * Width + j);

        public Image(int width, int height, TypeCode type) : base(width, height, type)
        {
            _baseArray = Array.CreateInstance(Type, Width * Height);
        }

        public Image(Array initialArray, int width, int height, bool copy = true) :
            base(
                width,
                height,
                initialArray is { } notNullArray
                    ? Type.GetTypeCode(notNullArray.GetType().GetElementType())
                    : throw new ArgumentNullException("Argument is null: " + nameof(initialArray))
            )

        {
            if (copy)
            {
                _baseArray = Array.CreateInstance(Type, width * height);
                Buffer.BlockCopy(initialArray, 0, _baseArray, 0, width * height * ItemSizeInBytes);
            }
            else
            {
                _baseArray = initialArray;
            }
        }

        public Image(byte[] initialArray, int width, int height, TypeCode type) 
            : this(
                (initialArray 
                 ?? throw new ArgumentNullException("Argument is null: " + nameof(initialArray)))
                .AsSpan(), 
                width, 
                height,
                type
            )
        {
        }

        public Image(ReadOnlySpan<byte> initialArray, int width, int height, TypeCode type)
            : this(width, height, type)

        {
            if (initialArray.IsEmpty)
                throw new ArgumentNullException("Argument is empty: " + nameof(initialArray));

            var len = Math.Min(initialArray.Length, width * height * ItemSizeInBytes);

            initialArray.Slice(0, len).CopyTo(UnsafeAsBytes());
        }

        [Obsolete("Use `" + nameof(ByteView) + "`.")]
        public override byte[] GetBytes()
        {
            var size = ItemSizeInBytes;
            var byteArray = new byte[Width * Height * size];

            Buffer.BlockCopy(_baseArray, 0, byteArray, 0, byteArray.Length);

            return byteArray;
        }

        public override ImageBase Copy()
            => new Image(ByteView(), Width, Height, UnderlyingType);

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

                    var thisArr = (float[]) _baseArray;
                    var otherArr = (float[]) other._baseArray;
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

                    var thisArr = (double[])_baseArray;
                    var otherArr = (double[])other._baseArray;
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

        public override int GetHashCode() => base.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Span<T> UnsafeAsSpan<T>() => (T[]) _baseArray;

        private protected override IImageConstructor GetConstructor() => Ctor;

        public static Image CreateTyped<T>(ReadOnlySpan<T> data, int width, int height) where T : unmanaged
        {
            return new(MemoryMarshal.AsBytes(data), width, height, Type.GetTypeCode(typeof(T)));
        }

        private class ImageCtor : IImageConstructor
        {
            public ImageBase CreateAndFill(int width, int height, TypeCode type, ImageInitializers.ImageInitializer initializer)
            {
                var image = new Image(width, height, type);
                initializer(image.UnsafeAsBytes());
                return image;
            }

            public ImageBase CreateAndFill<TState>(int width, int height, TypeCode type, ImageInitializers.ImageInitializer<TState> initializer, TState state)
            {
                var image = new Image(width, height, type);
                initializer(image.UnsafeAsBytes(), state);
                return image;
            }
        }
    }
}
