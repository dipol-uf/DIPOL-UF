using System;

namespace DIPOL_UF
{
    internal class PropagatingEventArgs<T> : EventArgs where T : ReactiveObjectEx
    {
        public T Content { get; }

        public PropagatingEventArgs(T content) => Content = content;
    }
}
