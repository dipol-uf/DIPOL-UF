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
            Test1();

            Console.ReadKey();
        }

        private static void Test1()
        {
            using (var client = new DIPOL_Remote.Classes.DipolClient("dipol-2"))
            {
                client.Connect();

                using (var camera = client.CreateRemoteCamera())
                //using (var camera = new Camera())
                {
                    camera.FanControl(ANDOR_CS.Enums.FanMode.FullSpeed);
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

                    int N = 2;

                    sets.SetAcquisitionMode(ANDOR_CS.Enums.AcquisitionMode.Kinetic);
                    sets.SetKineticCycle(N, 1.1f);
                    sets.SetAccumulationCycle(2, 1.1f);
                    sets.SetExposureTime(1.5f);
                    sets.SetReadoutMode(ANDOR_CS.Enums.ReadMode.FullImage);
                    sets.SetTriggerMode(ANDOR_CS.Enums.TriggerMode.Internal);
                    sets.SetImageArea(new Rectangle(1, 1, 512, 512));

                    sets.ApplySettings(out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing);

                    //using (var str = new System.IO.StreamWriter("test.xml"))
                    //    sets.Serialize(str.BaseStream);

                    Console.WriteLine(timing);

                    //int[] array = new int[N * 512 * 512];
                    //int first = 0;
                    //int test = 0;
                    //int last = 0;
                    //int first2 = 0;
                    //int last2 = 0;

                    System.Threading.CancellationTokenSource source = new System.Threading.CancellationTokenSource();

                    var task = camera.StartAcquistionAsync(source, 100);

                    //ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetTotalNumberImagesAcquired(ref test);
                    //ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetNumberNewImages(ref first, ref last);
                    //ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetNumberAvailableImages(ref first, ref last);


                    //var app = new System.Windows.Application();
                    //app.Run(new TestWindow(camera));

                    //var t = DateTime.Now;

                    //for (int i = first; i <= last; i++)
                    //    Console.WriteLine($"{i} \t {ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetImages(i, i, array, 512 * 512, ref first2, ref last2) == 20002} \t {array.Max()}");

                    //Console.WriteLine("{0:F3} s", (DateTime.Now - t).TotalSeconds / test);

                    task.Wait();
                }
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

        private static void LoopTest()
        {

            int N = 50_000_000;
            int M = 50;
            double[] array = new double[N];

            Random R = new Random();

            for (int i = 0; i < N; i++)
                array[i] = R.NextDouble();

            DateTime t = DateTime.Now;
            double sum = 0.0;

            Func<int, double> worker = (i) =>
            {
                double locSum = 0.0;
                for (int j = 0; j < N; j++)
                    locSum += 1.0 / (1.0 + Math.Exp(Math.Log(Math.Abs(array[j]))));
                return locSum;
            };

            t = DateTime.Now;
            sum = 0.0;
            for (int i = 0; i < M; i++)
            {
                for (int j = 0; j < N; j++)
                    sum += 1.0/(1.0+Math.Exp(Math.Log(Math.Abs(array[j]))));
            }
            Console.WriteLine("Serial   {0:E3} \t {1:E3}", (DateTime.Now - t).TotalSeconds, sum);

            t = DateTime.Now;
            sum = 0.0;
            for (int i = 0; i < M; i++)
            {
                sum += worker(i);
            }
            Console.WriteLine("Delegate {0:E3} \t {1:E3}", (DateTime.Now - t).TotalSeconds, sum);

            t = DateTime.Now;
            sum = 0.0;
            Parallel.For(0, M, () => 0.0,
            (i, state, local) =>
            {
                local += worker(i);
                return local;
            }, (item) => sum += item);
            Console.WriteLine("Parallel {0:E3} \t {1:E3}", (DateTime.Now - t).TotalSeconds, sum);

        }
     
    }
}
