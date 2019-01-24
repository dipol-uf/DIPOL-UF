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

            var model = new ProgressBar
            {
                Maximum = 140,
                Minimum = 30,
                Value = 0,
                BarTitle = "TestTitle",
                BarComment = "TestComment"
            };

            model.WhenErrorsChanged
              .Subscribe(x => Console.WriteLine(
                  $"\t\t\t\tModel: {x.PropertyName}: " +
                  $"{model.GetTypedErrors(x.PropertyName).FirstOrDefault().Message}"));
            model.WhenAnyPropertyChanged(nameof(model.HasErrors))
              .Subscribe(x => Console.WriteLine($"\t\t\t\tHas errors {x.HasErrors}"));

            model.WhenErrorsChanged.Where(x => x.PropertyName == nameof(model.Minimum))
                 .Subscribe(x => Console.WriteLine(
                     $"\t\t\t\t\tModel: {x.PropertyName}: " +
                     $"{model.GetTypedErrors(x.PropertyName).FirstOrDefault().Message}"));

            var vm = new ProgressBarViewModel(model);
            vm.WhenErrorsChanged
              .Subscribe(x =>
              {
                  var msg = vm.GetTypedErrors(x.PropertyName).FirstOrDefault().Message;
                  Console.WriteLine(
                      $"VM: {x.PropertyName}: " +
                      $"{msg}");
              });

            vm.WhenAnyPropertyChanged(nameof(vm.HasErrors))
              .Subscribe(x => Console.WriteLine($"VM Has errors {x.HasErrors}"));

            vm.WhenErrorsChanged.Where(x => x.PropertyName == nameof(vm.Minimum))
              .Subscribe(x => Console.WriteLine(
                  $"VM: {x.PropertyName}: " +
                  $"{vm.GetTypedErrors(x.PropertyName).FirstOrDefault().Message}"));


            Task.Run(() =>
            {
                while (!model.IsIndeterminate && model.Value < 100)
                {
                    model.Value++;
                    if(model.Value == model.Minimum)
                        Console.WriteLine("Correct values");
                    Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
                }
            });

            Task.Run(() =>
            {
                Task.Delay(TimeSpan.FromSeconds(7)).Wait();
                Console.WriteLine("\r\nTo indeterminate\r\n");
                model.IsIndeterminate = true;
                //model.Value = 55;
                model.BarComment = "New comment";
                Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                model.IsIndeterminate = false;
                model.DisplayPercents = true;

            });
            applicationInstance.Run(new Views.ProgressWindow(vm));
        }
    }
}
