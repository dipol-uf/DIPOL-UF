//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace DIPOL_Remote.Remote
{

    internal class AsyncResult : IAsyncResult, IDisposable
    {
        private readonly CancellationTokenSource _taskTokenSource;
        private readonly RemoteCancellationToken _remoteToken;

        private readonly ManualResetEventSlim _event;
        public bool IsCompleted => Task?.IsCompleted ?? false;
        public WaitHandle AsyncWaitHandle => _event.WaitHandle;
        public object AsyncState { get; }
        public bool CompletedSynchronously => false;
        public Task Task { get; }
        public AsyncCallback Callback { get; }

        public AsyncResult(Task task, AsyncCallback callback, object state)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            
            _event = new ManualResetEventSlim(false);
            AsyncState = state;

            Task.GetAwaiter().OnCompleted(FinalizeInvocation);
        }

        public AsyncResult(
            Task task, 
            CancellationTokenSource tokenSource, 
            RemoteCancellationToken remoteToken,
            AsyncCallback callback, object state)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));

            _taskTokenSource = tokenSource.Token.CanBeCanceled
                ? tokenSource
                : throw new ArgumentException("Provided token cannot be cancelled.", nameof(tokenSource));

            AsyncState = state;
            _remoteToken = remoteToken;
            _event = new ManualResetEventSlim(false);

            if (_remoteToken != RemoteCancellationToken.None)
                RemoteControl.CancellationRequested += NotifyCancelled;

            Task.GetAwaiter().OnCompleted(FinalizeInvocation);
        }

        private void FinalizeInvocation()
        {
            RemoteControl.CancellationRequested -= NotifyCancelled;
            _event.Set();
            Callback.Invoke(this);
        }

        internal void NotifyCancelled(
            object sender, 
            RemoteControl.CancellationRequestedEventArgs e)
        {
            if (e?.Token == _remoteToken)
            {
                RemoteControl.CancellationRequested -= NotifyCancelled;
                _taskTokenSource?.Cancel();
            }
            
        }


        public void Dispose()
        {
            RemoteControl.CancellationRequested -= NotifyCancelled;
            Task?.Dispose();
            _event?.Dispose();
        }
    }
}
