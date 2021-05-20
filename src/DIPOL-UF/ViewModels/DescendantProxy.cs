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
using System.Reactive;
using System.Reactive.Disposables;
using System.Windows.Input;
using DIPOL_UF.Models;
using ReactiveUI;

namespace DIPOL_UF.ViewModels
{


    internal class DescendantProxy : ReactiveObjectEx
    {
        public ICommand ViewFinished { get; }
        public ICommand WindowShown { get; }
        public event EventHandler ViewRequested;
        public event EventHandler ClosingRequested;

        public DescendantProxy(
            IObservable<ReactiveObjectEx> modelSource,
            IObservable<object> closingSource)
        {
            modelSource.ObserveOnUi()
                       .Subscribe(x => ViewRequested?.Invoke(this, new PropagatingEventArgs(x)))
                       .DisposeWith(Subscriptions);

            closingSource.ObserveOnUi()
                         .Subscribe(x => ClosingRequested?.Invoke(this, new EventArgs()))
                         .DisposeWith(Subscriptions);


            ViewFinished = ReactiveViewModelBase.DisposeFromViewCallbackCommand(Subscriptions);
        }

        public DescendantProxy(DescendantProvider provider,
            Func<ReactiveObjectEx, ReactiveViewModelBase> constructor)
        {
            if (provider is null)
                throw new ArgumentNullException(nameof(provider));

            if (constructor is null)
                throw new ArgumentNullException(nameof(constructor));

            provider.ViewRequested
                    .ObserveOnUi()
                    .Subscribe(x => 
                        ViewRequested?.Invoke(this, new PropagatingEventArgs(constructor(x))))
                    .DisposeWith(Subscriptions);

            provider.ClosingRequested
                    ?.ObserveOnUi()
                    .Subscribe(x => ClosingRequested?.Invoke(this, EventArgs.Empty))
                    .DisposeWith(Subscriptions);

            var shownCmd = ReactiveCommand.Create<Unit>(_ => { })
                                          .DisposeWith(Subscriptions);

            WindowShown = shownCmd;
                

            var finishedCmd = ReactiveViewModelBase
                .DisposeFromViewCallbackCommand(Subscriptions);

            ViewFinished = finishedCmd;

            if (!(provider.ViewFinished is null))
                finishedCmd.InvokeCommand(provider.ViewFinished).DisposeWith(Subscriptions);

            if (!(provider.WindowShown is null))
                shownCmd.InvokeCommand(provider.WindowShown).DisposeWith(Subscriptions);
        }
    }

}