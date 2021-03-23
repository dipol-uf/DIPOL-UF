using System;

namespace DipolImage
{
    internal interface IImageConstructor
    { 
        ImageBase CreateAndFill(int width, int height, TypeCode type, ImageInitializers.ImageInitializer initializer);
        ImageBase CreateAndFill<TState>(
            int width, int height, TypeCode type, ImageInitializers.ImageInitializer<TState> initializer, TState state
        );
    }
    
}