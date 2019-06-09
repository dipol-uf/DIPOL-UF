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
using ReactiveUI;

namespace DIPOL_UF.Models
{
    internal class DescendantProvider : ReactiveObjectEx
    {
        public ReactiveCommand<object, ReactiveObjectEx> ViewRequested { get; }
        public ReactiveCommand<Unit, Unit> ClosingRequested { get; }

        public ReactiveCommand<ReactiveObjectEx, Unit> ViewFinished { get; }
        public ReactiveCommand<Unit, Unit> WindowShown { get; }

        public DescendantProvider(
            ReactiveCommand<object, ReactiveObjectEx> requestView,
            ReactiveCommand<Unit, Unit> windowShown,
            ReactiveCommand<Unit, Unit> requestClosing,
            ReactiveCommand<ReactiveObjectEx, Unit> finalize)
        {
            if(requestView is null)
                throw new ArgumentNullException(nameof(requestView));
            ViewRequested = requestView.DisposeWith(Subscriptions);

            ClosingRequested = (requestClosing ??
                                ReactiveCommand.Create<Unit>(_ => { }))
                .DisposeWith(Subscriptions);

            ViewFinished = (finalize ??
                            ReactiveCommand.Create<ReactiveObjectEx, Unit>(_ => Unit.Default))
                .DisposeWith(Subscriptions);

            WindowShown = (windowShown ?? 
                           ReactiveCommand.Create<Unit>(_ => { }))
                .DisposeWith(Subscriptions);
        }
    }
}
