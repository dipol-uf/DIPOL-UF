using System;
using System.Threading;
using System.Threading.Tasks;

namespace DIPOL_Remote.Classes
{

    internal class AsyncVoidResult : IAsyncResult, IDisposable
    {
        private readonly ManualResetEventSlim _event;
        public bool IsCompleted => Task?.IsCompleted ?? false;
        public WaitHandle AsyncWaitHandle => _event.WaitHandle;
        public object AsyncState { get; }
        public bool CompletedSynchronously => false;
        public Task Task { get; }
        public AsyncCallback Callback { get; }

        public AsyncVoidResult(Task task, AsyncCallback callback, object state)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));

            _event = new ManualResetEventSlim(false);
            AsyncState = state;

            Task.GetAwaiter().OnCompleted(FinalizeInvocation);
        }

        private void FinalizeInvocation()
        {
            _event.Set();
            Callback.Invoke(this);
        }

        public void Dispose()
        {
            Task?.Dispose();
            _event?.Dispose();
        }
    }
}
