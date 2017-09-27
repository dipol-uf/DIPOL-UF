using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using ANDOR_CS;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;

using ImageDisplayLib;
using FITS_CS;

namespace ImageTest
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //try
            {
                // ContextSwitchTest();
                FITSTest();
                //Test1();
            }
            //catch(Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //}
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
                    
                    camera.SetTemperature(0);
                    camera.CoolerControl(ANDOR_CS.Enums.Switch.Enabled);
                    camera.TemperatureStatusChecked += (sender, e) => Console.WriteLine($"{e.Temperature} : {e.Status}");
                    camera.TemperatureMonitor(ANDOR_CS.Enums.Switch.Enabled, 1000);
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

                    int N = 15;

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

                    var st = camera.GetCurrentTemperature();
                    while (st.Temperature > 5 & st.Status == ANDOR_CS.Enums.TemperatureStatus.NotReached)
                    {
                        System.Threading.Thread.Sleep(200);
                        st = camera.GetCurrentTemperature();
                    }
                    

                    System.Threading.CancellationTokenSource source = new System.Threading.CancellationTokenSource();

                    var task = camera.StartAcquistionAsync(source, 100);

                    //ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetTotalNumberImagesAcquired(ref test);
                    //ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetNumberNewImages(ref first, ref last);
                    //ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetNumberAvailableImages(ref first, ref last);

                    System.Threading.Thread.Sleep(2000);
                    source.Cancel();

                    //var app = new System.Windows.Application();
                    //app.Run(new TestWindow(camera));

                    //var t = DateTime.Now;

                    //for (int i = first; i <= last; i++)
                    //    Console.WriteLine($"{i} \t {ANDOR_CS.Classes.AndorSDKInitialization.SDKInstance.GetImages(i, i, array, 512 * 512, ref first2, ref last2) == 20002} \t {array.Max()}");

                    //Console.WriteLine("{0:F3} s", (DateTime.Now - t).TotalSeconds / test);

                    task.Wait();
                    camera.CoolerControl(ANDOR_CS.Enums.Switch.Disabled);
                    camera.TemperatureMonitor(ANDOR_CS.Enums.Switch.Disabled);
                    camera.SetTemperature(20);
                }

                client.Disconnect();
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

            im.Clamp(100, 1000);
            im.Scale(int.MaxValue, int.MaxValue);
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

        private static void ContextSwitchTest()
        {
            DIPOL_Remote.Classes.DipolClient client = null;

            try
            {
                client = new DIPOL_Remote.Classes.DipolClient("dipol-2");
            
                client.Connect();

                int n = client.GetNumberOfCameras();
                if (n < 2)
                    throw new Exception($"Not enough cameras ({n})");

                CameraBase[] cams = new CameraBase[2];


                System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

                cams[0] = client.CreateRemoteCamera(0);
                cams[1] = client.CreateRemoteCamera(1);

                void TemperatureHandler(object sender, ANDOR_CS.Events.TemperatureStatusEventArgs e)
                {
                    if (sender is CameraBase cm)
                        Console.WriteLine($"{cm.SerialNumber} \t {e.Temperature} \t {e.Status}");
                    else
                        Console.WriteLine("ERROR! Sender is not a Camera");

                };

                foreach (var cam in cams)
                {
                    cam.TemperatureStatusChecked += TemperatureHandler;
                    cam.TemperatureMonitor(ANDOR_CS.Enums.Switch.Enabled, 1000);
                    cam.SetTemperature(0);
                    cam.CoolerControl(ANDOR_CS.Enums.Switch.Enabled);
                }


                watch.Stop();

               // Console.WriteLine(watch.Elapsed.TotalSeconds.ToString("E3"));

                Console.ReadKey();

                foreach (var cam in cams)
                    cam.CoolerControl(ANDOR_CS.Enums.Switch.Disabled);

                Console.ReadKey();

                for (int j = 0; j < cams.Length; j++)
                    cams[j].Dispose();
            }
            finally
            {
                client.Disconnect();
                client.Dispose();
            }
            
        }

        private static void FITSTest()
        {
            using (var str = new FITSStream(new System.IO.FileStream("test2.fits", System.IO.FileMode.Open)))
            {
                int count = 0;
                List<FITSUnit> keywords = new List<FITSUnit>();
                List<FITSUnit> data = new List<FITSUnit>();

                while (str.TryReadUnit(out FITSUnit u))
                {
                    if (u.IsKeywords)
                        keywords.Add(u);
                    else if (u.IsData)
                        data.Add(u);
                }

                var totalKeys = FITSKey.JoinKeywords(keywords.ToArray());

                var image = FITSUnit.JoinData<Int16>(data.ToArray());
                var dd = image.ToArray();

                int width = totalKeys.First(item => item.Header == "NAXIS1").GetValue<int>();
                int height = totalKeys.First(item => item.Header == "NAXIS2").GetValue<int>();

                double bScale = totalKeys.First(item => item.Header == "BSCALE").GetValue<double>();
                double bZero = totalKeys.First(item => item.Header == "BZERO").GetValue<double>();
                FITSImageType type = (FITSImageType)totalKeys.First(item => item.Header == "BITPIX").GetValue<int>();

                Image im = new Image(dd, width, height).CastTo<Int16, Single>(x => 1.0f * x);

                 im.MultiplyByScalar(bScale);
                 im.AddScalar(bZero); 
                 //im.MultiplyByScalar(0.5);
                 //im.AddScalar(16385);
                 //im.MultiplyByScalar(0.5);
                 //im = new Image(Enumerable.Range(0, 16).Select(i =>  Convert.ToUInt16( i * 2048) ).ToArray(), 16, 1);
                 var app = new System.Windows.Application();
                 app.Run(new TestWindow(im));
            }
        }
    }
}
