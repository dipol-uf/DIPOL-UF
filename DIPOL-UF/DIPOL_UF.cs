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
using System.Reactive.Linq;
using System.Threading.Tasks;
using DIPOL_UF.Models;
using DIPOL_UF.ViewModels;
using DynamicData.Binding;
using Newtonsoft.Json.Linq;
using SettingsManager;


namespace DIPOL_UF
{
    public static class DIPOL_UF_App
    {
        [STAThread]
        private static int Main(string[] args)
        {
            //System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
            //System.Diagnostics.Debug.AutoFlush = true;
            //System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            //System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            //var applicationInstance = new App();
            //applicationInstance.InitializeComponent();


            //using (var mainModel = new Models.DipolMainWindow())
            //{
            //    var view = new ViewModels.DipolMainWindowViewModel(mainModel);

            //    applicationInstance.Run(new Views.DipolMainWindow(view));

            //}

            Test();

            return 0;
        }

        static DIPOL_UF_App()
        {
            
        }

        private static void Test()
        {
            var applicationInstance = new App();
            applicationInstance.InitializeComponent();

            var model = new ProgressBar();
            model.WhenPropertyChanged(x => x.Value).Subscribe(x => Console.WriteLine($"Value is {x.Value}"));


            model.Maximum = 100;
            model.Minimum = 0;
            model.Value = 100;
            model.BarTitle = "TestTitle";
            model.BarComment = "TestComment";

            var vm = new ProgressBarViewModel(model);

            model.WhenPropertyChanged(x => x.HasErrors)
                 .Subscribe(x => Console.WriteLine(x.Value ? "Errors" : "No Errors"));

            Observable.FromEventPattern<DataErrorsChangedEventArgs>(
                          x => model.ErrorsChanged += x,
                          x => model.ErrorsChanged -= x)
                      .Subscribe(x => Console.WriteLine(x.EventArgs.PropertyName));

            model.ErrorsChanged += (sender, args) => Console.WriteLine($"Event {args.PropertyName}");

            //model.WhenPropertyChanged(x => x.Value)
            //  .Subscribe(x => Console.WriteLine($"VM Value is {x.Value}"));

            vm.WhenAnyPropertyChanged(nameof(vm.Value)).
              Subscribe(x => Console.WriteLine($"\t\tVM Value is {x.Value}"));

            vm.WhenPropertyChanged(x => x.HasErrors)
                .Subscribe(x => Console.WriteLine("\t\tVM " + (x.Value ? "Errors" : "No Errors")));
            Observable.FromEventPattern<DataErrorsChangedEventArgs>(
                          x => vm.ErrorsChanged += x,
                          x => vm.ErrorsChanged -= x)
                      .Subscribe(x => Console.WriteLine("\t\tVM " + x.EventArgs.PropertyName));
            vm.ErrorsChanged += (sender, args) => Console.WriteLine($"\t\tVM Event {args.PropertyName}");

            Task.Run(() =>
            {
                while (!model.IsIndeterminate && model.Value < 116)
                {
                    model.Value++;
                    Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
                }
            });

            Task.Run(() =>
            {
                Task.Delay(TimeSpan.FromSeconds(1.5)).Wait();
                //model.IsIndeterminate = true;
                model.Value = 95;
                model.BarComment = "New comment";
                Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                model.IsIndeterminate = false;
                model.DisplayPercents = true;


            }).Wait();
            Console.ReadKey();
            //applicationInstance.Run(new Views.ProgressWindow(vm));
        }
    }
}
