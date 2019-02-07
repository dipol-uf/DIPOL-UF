//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DIPOL_UF.Models;
using DIPOL_UF.ViewModels;
using DynamicData;
using DynamicData.Binding;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using SettingsManager;


namespace DIPOL_UF
{
    public static class DIPOL_UF_App
    {
        [STAThread]
        private static int Main(string[] args)
        {
            System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
            System.Diagnostics.Debug.AutoFlush = true;
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            //Test();
            //return 0;
          

            var applicationInstance = new App();
            applicationInstance.InitializeComponent();


            using (var mainModel = new Models.DipolMainWindow())
                using (var view = new ViewModels.DipolMainWindowViewModel(mainModel))
                    applicationInstance.Run(new Views.DipolMainWindow().WithDataContext(view));


            //Test();

            return 0;
        }

        private static void Test()
        {
            var disp = new CompositeDisposable();
            var command = ReactiveCommand
                .Create<int, int>(x => x)
                .DisposeWith(disp);

            var s = new Subject<bool>();
            var obs = command.AsObservable().Buffer(s);
            obs.Select(x => x.ToStringEx()).LogObservable("Second", disp);
           
            //obs.Select(x => x.EnumerableToString()).LogObservable("First", disp);


            command.Execute(5).Subscribe().DisposeWith(disp);
            command.Execute(6).Subscribe().DisposeWith(disp);
            command.Execute(115).Subscribe().DisposeWith(disp);

            command.Execute(-5).Subscribe().DisposeWith(disp);
            s.OnNext(false);
            command.Execute(-1235).Subscribe().DisposeWith(disp);

            //s.OnNext(true);

            Console.ReadKey();
        }
    }
}
