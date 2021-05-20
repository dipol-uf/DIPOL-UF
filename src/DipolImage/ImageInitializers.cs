using System;

namespace DipolImage
{
    public class ImageInitializers
    {
        public delegate void ImageInitializer(Span<byte> view);
        public delegate void ImageInitializer<in TState>(Span<byte> view, TState state);
    }
}