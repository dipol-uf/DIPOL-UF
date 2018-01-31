using System;
using System.Threading.Tasks;
using System.Windows;
using DipolImage;
using DIPOL_UF.Models;
using DIPOL_UF.ViewModels;

namespace Tests
{
    public class DebuggingTests
    {
        [STAThread]
        public static int Main()
        {
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
                var model = new DipolImagePresenter();
                var viewModel = new DipolImagePresnterViewModel(model);
                var wind = new DebugWindow()
                {
                    TestPresenter = {DataContext = viewModel}
                };

                var app = new Application();

                Task.Run(() =>
                {
                    Task.Delay(500).Wait();
                   model.LoadImage(TestImageUInt16);
                });
                app.Run(wind);
              

            }
        }
    }
}
