using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace DIPOL_Remote.Classes
{
    public class AsyncCameraResult : IAsyncResult, IDisposable
    {
        private readonly ManualResetEventSlim _event;
        public bool IsCompleted => Task?.IsCompleted ?? false;
        public WaitHandle AsyncWaitHandle => throw new NotSupportedException();
        public object AsyncState { get; }
        public bool CompletedSynchronously => false;
        public Task Task { get; }

        public AsyncCameraResult(Task cameraCreationTask, object state)
        {
            _event = new ManualResetEventSlim(false);
            AsyncState = state;

            Task = cameraCreationTask;
            if(cameraCreationTask?.IsCompleted == true)
                _event.Set();
            else
                Task.ConfigureAwait(false).GetAwaiter().OnCompleted(() => _event.Set());
        }

        public void Dispose()
        {
            Task?.Dispose();
            _event?.Dispose();
        }
    }
}
