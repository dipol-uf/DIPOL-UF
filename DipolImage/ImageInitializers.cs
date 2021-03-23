using System;

namespace DipolImage
{
    internal class ImageInitializers
    {
        internal delegate void ImageInitializer(Span<byte> view);
        internal delegate void ImageInitializer<in TState>(Span<byte> view, TState state);
    }
}