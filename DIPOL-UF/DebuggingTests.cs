using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
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
                var viewModel = new DipolImagePresnterViewModel(model);
                var wind = new DebugWindow()
                {
                    TestPresenter = {DataContext = viewModel}
                };
                var buffer = new byte[1024  * 512 * sizeof(ushort)];
                
                var r = new Random();
                var t = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(3000),
                    IsEnabled = false
                };


                t.Tick += (sender, e) =>
                {
                    r.NextBytes(buffer);
                    model.LoadImage(new Image(buffer, 1024, 512, TypeCode.UInt16));
                    //TextExtension.UpdateUICullture(new CultureInfo("en-US"));
                    t.Stop();
                };
                t.Start();
                app.Run(wind);
              

            }
        }
    }
}
