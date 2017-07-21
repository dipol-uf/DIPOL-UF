using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using SDKInit = ANDOR_CS.Classes.AndorSDKInitialization;
using SDK = ATMCD64CS.AndorSDK;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;

using ANDOR_CS.Classes;

namespace ANDOR_CS
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new DIPOL_Remote.DipolHost();
            host.Host();
            Console.ReadKey();
            host.Dispose();

            //var t = System.Diagnostics.Stopwatch.StartNew();

            //WriteToDiskTest(1000);

            //t.Stop();

            //Console.WriteLine("Total time {0:F6}", t.ElapsedMilliseconds / 1000.0);

            //Console.ReadKey();

            

               // TestAcquisitionSettings();

              

        }

        public static void TestAcquisitionSettings()
        {

            using (var cam = new Camera())
            {
                cam.FanControl(FanMode.Off);

                var settings = cam.GetAcquisitionSettingsTemplate();

                settings.SetAcquisitionMode(AcquisitionMode.SingleScan);

                settings.SetOutputAmplifier(OutputAmplification.Conventional);
                settings.SetADConverter(0);
                //foreach (var speed in settings.GetAvailableHSSpeeds())
                //    Console.WriteLine("Speed {0} has value {1}", speed.Item1 + 1, speed.Item2);

                var query = settings.GetAvailableHSSpeeds();

                settings.SetHSSpeed(1);
                settings.SetVSSpeed(4);

                settings.SetExposureTime(0.5f);

                settings.SetImageArea(new Rectangle(1, 1, 512, 512));

                settings.SetReadoutMode(ReadMode.FullImage);

                settings.SetTriggerMode(TriggerMode.Internal);

                var output = settings.ApplySettings(out (float, float, float, int) timing);

                foreach (var o in output)
                    Console.WriteLine(o);


                Console.WriteLine(timing);

                int n = 10;

                List<float[]> images = new List<float[]>(n);

                for (int i = 0; i < n; i++)
                    images.Add(new float[cam.Properties.DetectorSize.Horizontal * cam.Properties.DetectorSize.Vertical]);

                TimeSpan span = default(TimeSpan);
                DateTime startTime = DateTime.Now;

                cam.AcquisitionStarted += (c, e) =>
               {
                   startTime = e.EventTime;
                   Console.WriteLine("Acquisition started  on {0:hh-mm-ss.ffffff}", e.EventTime);
               };
                cam.AcquisitionFinished += (c, e) =>
                {
                    span = e.EventTime - startTime;
                    Console.WriteLine("Acquisition finished on {0:hh-mm-ss.ffffff}", e.EventTime);
                };

                while (true)
                {
                    Console.WriteLine("----------------------------------------\r\n");

                    var t = System.Diagnostics.Stopwatch.StartNew();
                    var cancel = new System.Threading.CancellationTokenSource();
                    for (int i = 0; i < n; i++)
                    {


                        Console.WriteLine($"\r\nExposure {i + 1}");

                        cam.StartAcquistionAsync(cancel.Token, 5).Wait();
                        //Task.Delay(100).Wait();
                        Console.WriteLine("Exposure time {0:F6}", span.TotalMilliseconds / 1000.0);

                        ANDOR_CS.Exceptions.AndorSDKException.ThrowIfError(SDKInit.SDKInstance.GetAcquiredFloatData(images[i], (uint)images[i].Length), "");
                    }
                    t.Stop();

                    Console.WriteLine("{0:F4} s", t.ElapsedMilliseconds / 1000.0);
                    var key = Console.ReadKey();

                    if (key.Key == ConsoleKey.Escape)
                        break;
                }

            }
        }

        public static void WriteToDiskTest(int n = 10)
        {
            UInt16[] arr = new UInt16[512 * 512];
            Random r = new Random();

            for (int i = 0; i < arr.Length; i++)
                arr[i] = (UInt16)r.Next(500, 1000);

            byte[][] bytes = new byte[n][];

            for (int i = 0; i < n; i++)
                bytes[i] = new byte[arr.Length * 2];

            var t = System.Diagnostics.Stopwatch.StartNew();

           //for (int i = 0; i < n; i++)
            Parallel.For(0, n, (i) =>
            {
                for (int k = 0; k < arr.Length; k++)
                {
                    var temp = BitConverter.GetBytes(arr[k]);
                    bytes[i][2 * k] = temp[0];
                    bytes[i][2 * k + 1] = temp[1];
                }

                using (var str = new FileStream($@".\Output\file_{i + 1}.dat", FileMode.OpenOrCreate))
                    str.Write(bytes[i], 0, bytes[i].Length);

            }
            );
            t.Stop();

            Console.WriteLine("Writing time {0:F6}", t.ElapsedMilliseconds / 1000.0);
            Console.WriteLine("Writing time per file, avg {0:F6}", t.ElapsedMilliseconds / 1000.0 / n);
        }
  
        public static void TestMonitor()
        {
            using (var cam = new Camera())
            {
                cam.FanControl(FanMode.LowSpeed);
                cam.SetTemperature(-10);
                cam.CoolerControl(Switch.Enabled);

               

                cam.TemperatureStatusChecked += (sender, e) =>
                {
                    Console.Write("\r[{0:hh-mm-ss.fff}] {1:F2}",
                      e.EventTime, e.Temperature);

                    foreach (var name in Extensions.GetEnumNames(typeof(TemperatureStatus), e.Status))
                        Console.Write($" {name}");
                    //Console.WriteLine();
                };

                cam.TemperatureMonitor(Switch.Enabled, 250);
               
                Console.ReadKey();

                cam.SetTemperature(10);

                Console.ReadKey();


                cam.CoolerControl(Switch.Disabled);

            }
        }

        public static void SerializationTest()
        {


            var settings = new AcquisitionSettings();

            using (var str = new MemoryStream(2600))
            {
                settings.Serialize(str, "Camera 1");
                str.Position = 0;

              
                settings.Deserialize(str);
            }
            //Console.WriteLine(Extensions.XmlWriteValueTuple(settings.Amplifier));
            //var ser = new System.Runtime.Serialization.DataContractSerializer(settings.GetType());

            //System.Xml.XmlWriterSettings s = new System.Xml.XmlWriterSettings() { Indent = true };

            //using (var str = System.Xml.XmlWriter.Create("test.dat", s))
            //{
            //    ser.WriteObject(str, settings);
            //}
            ////ser.Serialize(Console.Out, settings);

            //Console.WriteLine();
            Console.ReadKey();
        }

        public static void TestParallel(int N = 10)
        {
            Task[] task = new Task[N];

            for (int i = 0; i < N; i++)
                task[i] = Task.Run(() =>
                {
                    int num = 0;
                    SDKInit.Call(SDKInit.SDKInstance.GetNumberDevices, out num);
                    Console.WriteLine($"{num}");
                }
                );

            Task.WaitAll(task);

            Console.ReadKey();
        }

        #region MISC
        //public static void Test()
        //{
        //    using (var cam = new Camera())
        //    {
        //        var temp1 = cam.GetCurrentTemperature();
        //        uint result = 0;
        //        result = SDKInit.SDKInstance.SetVSSpeed(1);
        //        // EM-amp
        //        result = SDKInit.SDKInstance.SetHSSpeed(1, 1);

        //        // Frame transfer ON
        //        result = SDKInit.SDKInstance.SetFrameTransferMode(1);

        //        // Single Scan
        //        result = SDKInit.SDKInstance.SetAcquisitionMode(3);

        //        int n = 3;
        //        // 3 images in a series
        //        result = SDKInit.SDKInstance.SetNumberKinetics(n);

        //        // No accumulation
        //        result = SDKInit.SDKInstance.SetNumberAccumulations(1);

        //        result = SDKInit.SDKInstance.SetExposureTime(1.0f);

        //        // Image
        //        result = SDKInit.SDKInstance.SetReadMode(4);

        //        int x = 0, y = 0;
        //        result = SDKInit.SDKInstance.GetDetector(ref x, ref y);

        //        result = SDKInit.SDKInstance.SetImage(1, 1, 1, x, 1, y);

        //        var now = DateTime.Now;
        //        result = SDKInit.SDKInstance.StartAcquisition();
        //        result = SDKInit.SDKInstance.WaitForAcquisition();

        //        System.Threading.Thread.Sleep(5000);

        //        //result = SDKInit.SDKInstance.SaveAsSif(".\\test.sif");

        //        //result = SDKInit.SDKInstance.SaveAsRaw(".\\test.raw", 3);

        //        //result = SDKInit.SDKInstance.SaveAsFITS(".\\test.fits", 2);

        //        float[] array = new float[x * y * n];

        //        result = SDKInit.SDKInstance.GetAcquiredFloatData(array, (uint)array.Length);

        //        var imgData = GetImage(array.Take(x * y).Select((p) => (double)p).ToArray(), x, y);

        //        List<GetFITS.Keyword> keys = new List<GetFITS.Keyword>();
        //        keys.Add(new GetFITS.Keyword("TIME", GetFITS.KeywordType.String, now.ToString("yyyy-MM-dd hh:mm:ss.ffffff"), "Custom keyword"));

        //        var block = GetFITS.FITSBlock.FITSFromImage(imgData, GetFITS.PixType.FLOAT_32, keys);

        //        using (var file = GetFITS.FITSFile.CreateNew(new System.IO.FileStream("newfits.fits", System.IO.FileMode.OpenOrCreate)))
        //        {
        //            file.AssignHDUs(block);

        //            file.WriteToStream();
        //        }

        //        Console.WriteLine(result == SDK.DRV_SUCCESS ? "Success!" : "Failed!");

        //    }

        //}

        //public static double[,] GetImage(double[] data, int n, int m)
        //{
        //    double[,] result = new double[n, m];

        //    for (int i = 0; i < n; i++)
        //        for (int j = 0; j < m; j++)
        //            result[i, j] = data[j + m * i];

        //    return result;
        //}

        //public static void Test2()
        //{
        //    using (var cam = new Camera())
        //    {
        //        cam.FanControl(FanMode.Off);
        //    }

        //}

        //public static void Test3()
        //{
        //    using (Camera cam = new Camera())
        //    {
        //        var x = cam.Capabilities;

        //        cam.FanControl(FanMode.Off);

        //        var t = cam.GetCurrentTemperature();

        //        Console.WriteLine("\r\nAcquisition Modes:");
        //        foreach (var val in Extensions.GetEnumNames(typeof(AcquisitionMode), x.AcquisitionModes).Skip(1))
        //            Console.WriteLine("\t> " + val);

        //        Console.WriteLine("\r\nRead Modes:");
        //        foreach (var val in Extensions.GetEnumNames(typeof(ReadMode), x.ReadModes).Skip(1))
        //            Console.WriteLine("\t> " + val);

        //        Console.WriteLine("\r\nFT Read Modes:");
        //        foreach (var val in Extensions.GetEnumNames(typeof(ReadMode), x.FTReadModes).Skip(1))
        //            Console.WriteLine("\t> " + val);

        //        Console.WriteLine("\r\nTrigger Modes:");
        //        foreach (var val in Extensions.GetEnumNames(typeof(TriggerMode), x.TriggerModes).Skip(1))
        //            Console.WriteLine("\t> " + val);

        //        Console.WriteLine("\r\nCamera Type:");
        //        Console.WriteLine("\t> " + x.CameraType);

        //        Console.WriteLine("\r\nPixel Modes:");
        //        foreach (var val in Extensions.GetEnumNames(typeof(PixelMode), x.PixelModes))
        //            Console.WriteLine("\t> " + val);

        //        Console.WriteLine("\r\nSet Functions:");
        //        foreach (var val in Extensions.GetEnumNames(typeof(SetFunction), x.SetFunctions).Skip(1))
        //            Console.WriteLine("\t> " + val);

        //        Console.WriteLine("\r\nGet Functions:");
        //        foreach (var val in Extensions.GetEnumNames(typeof(GetFunction), x.GetFunctions).Skip(1))
        //            Console.WriteLine("\t> " + val);

        //        Console.WriteLine("\r\nFeatures:");
        //        foreach (var val in Extensions.GetEnumNames(typeof(SDKFeatures), x.Features).Skip(1))
        //            Console.WriteLine("\t> " + val);

        //        Console.WriteLine("\r\nEM Gain:");
        //        foreach (var val in Extensions.GetEnumNames(typeof(EMGain), x.EMGainFeatures).Skip(1))
        //            Console.WriteLine("\t> " + val);

        //        Console.WriteLine();
        //    }
        //}

        //public static void Test4()
        //{
        //    using (var cam = new Camera())
        //    {
        //        var settings = cam.GetAcquisitionSettingsTemplate();

        //    }
        //}

        //public static void TestGetAplifierDesc()
        //{
        //    using (var cam = new Camera())
        //    {
        //        cam.FanControl(FanMode.Off);

        //        string s = "";

        //        var result = SDKInit.SDKInstance.GetAmpDesc(0, ref s, 21);
        //    }
        //}

        //public static void TestQuery()
        //{
        //    int[] arr = new int[10];

        //    for (int i = 0; i < arr.Length; i++)
        //        arr[i] = i * i;

        //    Console.WriteLine(arr.IndexOf(0));
        //    Console.WriteLine(arr.IndexOf(4));
        //    Console.WriteLine(arr.IndexOf(5));
        //}
        #endregion
    }
}
