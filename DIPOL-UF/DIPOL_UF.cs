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
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DIPOL_UF.Models;
using DIPOL_UF.ViewModels;
using ReactiveUI;
using Tests;


namespace DIPOL_UF
{
    public static class DIPOL_UF_App
    {
        [STAThread]
        private static int Main(string[] args)
        {

            //System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
            System.Diagnostics.Debug.AutoFlush = true;
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            Test();
            return 0;



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

            var arr = new byte[256 * 512 * sizeof(ushort)];
            var r = new Random();
            r.NextBytes(arr);

            var img = new DipolImage.Image(arr, 512, 256, TypeCode.UInt16);


            var app = new App();
            app.InitializeComponent();

            var model = new DipolImagePresenter();
            var vm = new DipolImagePresenterViewModel(model);
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                await model.LoadImageCommand.Execute(img);
            });

            var view = new DebugWindow() {DataContext = vm};

            app.Run(view);

        }
    }
}
