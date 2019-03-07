using System;
using System.ComponentModel;

namespace DIPOL_UF
{
    internal class ReactiveWrapper<T> : ReactiveObjectEx where T : INotifyPropertyChanged, IDisposable
    {
        public T Object { get; set; }

        public ReactiveWrapper(T @object)
        {
            Object = @object;
        }

        protected override void Dispose(bool disposing)
        {
            if(!IsDisposed)
                if(disposing)
                    Object?.Dispose();

            base.Dispose(disposing);
        }

    }
}
