using System;

namespace DipolImage
{
    internal interface IImageConstructor
    {
        ImageBase CreateTyped<T>(ReadOnlySpan<T> data, int width, int height) where T : unmanaged;
        ImageBase CreateAndFill(int width, int height, TypeCode type, ImageInitializers.ImageInitializer initializer);
        ImageBase CreateAndFill<TState>(
            int width, int height, TypeCode type, ImageInitializers.ImageInitializer<TState> initializer, TState state
        );
    }
    
}