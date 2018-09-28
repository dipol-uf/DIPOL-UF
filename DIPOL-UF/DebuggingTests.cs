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
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ANDOR_CS.Classes;
using DipolImage;
using DIPOL_UF;
using DIPOL_UF.Models;
using DIPOL_UF.ViewModels;
//using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Tests
{
    public class DebuggingTests
    {
        [STAThread]
        public static int Main()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            new Debugger().DisplayImage();
            return 0;
        }

        public class Debugger
        {
            public Image TestImageUInt16;

            public Debugger()
            {
                var initArr = new ushort[256 * 512];
                for (var i = 0; i < 256; i++)
                    for (var j = 0; j < 512; j++)
                        initArr[i * 512 + j] = (ushort)(Math.Pow(i + j, 1.5));

                TestImageUInt16 = new Image(initArr, 512, 256);
            }

            public void DisplayImage()
            {
                var app = new App();
                app.InitializeComponent();
                var model = new DipolImagePresenter();
                var viewModel = new DipolImagePresenterViewModel(model);
                var wind = new DebugWindow()
                {
                    //TestPresenter = { DataContext = viewModel }
                };
                var buffer = new byte[1024  * 512 * sizeof(ushort)];
                
                var r = new Random();
                var t = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(3000),
                    IsEnabled = false
                };


                //t.Tick += (sender, e) =>
                //{
                //    r.NextBytes(buffer);
                //    model.LoadImage(new Image(buffer, 1024, 512, TypeCode.UInt16));
                //    TextExtension.UpdateUiCulture(new CultureInfo("ru-RU"));
                //    t.Stop();
                //};
                //t.Start();
                app.Run(wind);
              

            }

            public void TestAcqSettings()
            {
                using (var setts = new AcquisitionSettings())
                    using (var memStr = new MemoryStream())
                    {
                        var writer = new StreamWriter(memStr);
                        setts.WriteJson(writer);

                        var reader = new StreamReader(memStr);
                        reader.BaseStream.Position = 0;
                        while (!reader.EndOfStream)
                            Console.WriteLine(reader.ReadLine());
                        reader.BaseStream.Position = 0;

                        setts.ReadJson(reader);
                    }

                Console.ReadKey();
            }
        }
    }
}
