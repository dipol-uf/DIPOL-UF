using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;

using ImageDisplayLib;

namespace ImageTest
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Test2();

            Console.ReadKey();
        }

        private static void Test1()
        {
            using (var camera = new Camera())
            {
                camera.NewImageReceived += (sender, arg) => Console.WriteLine($"New image received at {{0:HH-mm-ss.fff}}: {arg.First} {arg.Last}", arg.EventTime);
                var sets = camera.GetAcquisitionSettingsTemplate();

                sets.SetADConverter(0);
                sets.SetOutputAmplifier(ANDOR_CS.Enums.OutputAmplification.Conventional);

                var speeds = sets.GetAvailableHSSpeeds();

                foreach (var speed in speeds)
                    Console.WriteLine(speed);

                sets.SetHSSpeed(1);

                var preAmps = sets.GetAvailablePreAmpGain();

                Console.WriteLine();

                foreach (var amp in preAmps)
                    Console.WriteLine(amp);

                sets.SetPreAmpGain(0);

                int N = 10;

                sets.SetAcquisitionMode(ANDOR_CS.Enums.AcquisitionMode.Kinetic);
                sets.SetKineticCycle(N, 0.1f);
                sets.SetAccumulationCycle(2, 0.1f);
                sets.SetExposureTime(0.5f);
                sets.SetReadoutMode(ANDOR_CS.Enums.ReadMode.FullImage);
                sets.SetTriggerMode(ANDOR_CS.Enums.TriggerMode.Internal);
                sets.SetImageArea(new Rectangle(1, 1, 512, 512));

                sets.ApplySettings(out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing);

                Console.WriteLine(timing);

                int[] array = new int[N * 512 * 512];
                int first = 0;
                int test = 0;
                int last = 0;
                int first2 = 0;
                int last2 = 0;

                camera.StartAcquistionAsync(System.Threading.CancellationToken.None, 100).Wait();
                ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetTotalNumberImagesAcquired(ref test);
                ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetNumberNewImages(ref first, ref last);
                ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetNumberAvailableImages(ref first, ref last);


                var t = DateTime.Now;

                for (int i = first; i <= last; i++)
                    Console.WriteLine($"{i} \t {ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetImages(i, i, array, 512 * 512, ref first2, ref last2) == 20002} \t {array.Max()}");

                Console.WriteLine("{0:F3} s", (DateTime.Now - t).TotalSeconds / test);
            }
        }

        private static void Test2()
        {


            var app = new System.Windows.Application();
            app.Run(new TestWindow());
        }

        private static void ArrayTest()
        {

            int N = 100000000;
            Array arr1 = Array.CreateInstance(typeof(int), N);
            Image im = new Image(arr1, N, 1);

            double sum = 0;
            var t = DateTime.Now;


            for (int j = 0; j < 10; j++)
            {
                sum = 0;
                t = DateTime.Now;

                for (int i = 0; i < N; i++)
                    sum += im.Get<int>(0, i);

                Console.WriteLine("{0:E3}", (DateTime.Now - t).TotalSeconds / N);
            }

            var nIm = im.Clamp(100, 10000).Scale();
        }
     
    }
}
