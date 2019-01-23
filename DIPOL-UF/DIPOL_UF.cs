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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DIPOL_UF.Models;
using DIPOL_UF.ViewModels;
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
            model.Maximum.Value = 100;
            model.Minimum.Value = -20;
            model.Value.Value = 53;
            model.BarTitle.Value = "TestTitle";
            model.BarComment.Value = "TestComment";
            var vm = new ProgressBarViewModel(model);

            Task.Run(() =>
            {
                while(!model.IsIndeterminate.Value && model.TryIncrement())
                    Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();
            });

            Task.Run(() =>
            {
                Task.Delay(TimeSpan.FromSeconds(3)).Wait();
                model.IsIndeterminate.Value = true;
            });

            applicationInstance.Run(new Views.ProgressWindow(vm));
        }
    }
}
