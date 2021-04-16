using System;

namespace DipolImage
{
    internal interface IImageConstructor
    { 
        Image CreateAndFill(int width, int height, TypeCode type, ImageInitializers.ImageInitializer initializer);
        Image CreateAndFill<TState>(
            int width, int height, TypeCode type, ImageInitializers.ImageInitializer<TState> initializer, TState state
        );
    }
    
}