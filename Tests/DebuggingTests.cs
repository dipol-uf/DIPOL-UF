using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DipolImage;
using DIPOL_UF.Models;
using DIPOL_UF.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlot;
using OxyPlot.Series;


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
            //public Image TestImageUInt16;

            public Debugger()
            {
                //var initArr = new ushort[256 * 512];
                //for (var i = 0; i < 256; i++)
                //    for (var j = 0; j < 512; j++)
                //        initArr[i * 512 + j] = (ushort)(Math.Pow(i + j, 1.5));

                //TestImageUInt16 = new Image(initArr, 512, 256);
            }

            public void DisplayImage()
            {

                List<DataPoint> points = new List<DataPoint>();

                var r = new Random();

                var data = Enumerable.Range(0, 10000)
                                     .Select(i => Math.Pow(1e3*i, 2)*Math.Exp(-i*1e-16) * r.NextDouble())
                                     .ToList();

                var min = data.Min();
                var max = data.Max();
                var N = 1000;

                var dx = (max - min) / N;
                for (var i = 0; i < N; i++)
                {
                    var count = data.Count(x => x > (min + i * dx) && x < (min + (i + 1) * dx));
                    points.Add(new DataPoint(min + dx * (i+0.5), count));
                }


                var app = new Application();
                var wind = new Tests.DebugWindow()
                {
                    DataContext = points
                };
                DispatcherTimer t = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromSeconds(10)
                };

              
                t.Start();
              
                app.Run(wind);


            }
        }
    }
}
