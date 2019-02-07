using System;

namespace DIPOL_UF
{
    internal class PropagatingEventArgs : EventArgs
    {
        public ReactiveObjectEx Content { get; }

        public PropagatingEventArgs(ReactiveObjectEx content) 
            => Content = content ?? throw new NullReferenceException(nameof(content));
    }
}
