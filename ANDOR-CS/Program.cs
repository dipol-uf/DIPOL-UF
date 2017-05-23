using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDKInit = ANDOR_CS.AndorSDKInitialization;
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

            TestAcquisitionSettings();
        }

       
        public static void TestAcquisitionSettings()
        {
            using (var cam = new Camera()) 
            {
                cam.FanControl(FanMode.Off);

                var settings = cam.GetAcquisitionSettingsTemplate();

               settings.SetOutputAmplifier(OutputAmplification.ElectromMultiplication);
               settings.SetADConverter(0);
               foreach (var speed in settings.GetAvailableHSSpeeds())
                    Console.WriteLine("Speed {0} has value {1}", speed.Item1 + 1, speed.Item2);

                var query = settings.GetAvailableHSSpeeds();

                settings.SetHSSpeed(query.First().Item1);

                foreach (var gain in settings.GetAvailablePreAmpGain())
                    Console.WriteLine("Gain {0} has name {1}", gain.Item1 + 1, gain.Item2);

                settings.SetVSSpeed();
                settings.SetVSAmplitude(VSAmplitude.Normal);

                settings.SetPreAmpGain(settings.GetAvailablePreAmpGain().First().Item1);

                settings.SetAcquisitionMode(AcquisitionMode.SingleScan);
                settings.SetReadoutMode(ReadMode.FullImage);
                settings.SetTriggerMode(TriggerMode.Internal);

                settings.SetImageArea(new Rectangle(new Point2D(1, 1), cam.Properties.DetectorSize));

                settings.SetExposureTime(10.0f);

                int counter = 0;

                cam.AcquisitionStarted += (c, e) => Console.WriteLine("\r\nAcquisition started on camera {0}", (c as Camera).CameraModel);
                cam.AcquisitionStatusChecked += (c, e) =>  Console.Write("\rAcquiring {0}{1}", new string('.', 1 + (counter++ %3)), new string(' ', 3 - (counter % 3)));
                cam.AcquisitionFinished += (c, e) => Console.WriteLine("\r\nAcquisition finished on camera {0}", (c as Camera).CameraModel);

                var result = settings.ApplySettings(out var timing);

                Console.WriteLine();

                foreach (var r in result)
                    Console.WriteLine(r);

                Console.WriteLine($"\r\nExposure: {timing.ExposureTime}\t Accumulation: {timing.AccumulateCycleTime}\t Kinetic: {timing.KineticCycleTime}");

                if (cam.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                {
                    var temp = cam.GetCurrentTemperature();

                    Console.WriteLine($"\r\nTemp: {Extensions.GetEnumNames(typeof(TemperatureStatus), temp.Status).First()}\t{temp.Temperature} degrees");
                }

                cam.StartAcquistionAsync().Wait();                                

                float[] array = new float[cam.Properties.DetectorSize.Horizontal * cam.Properties.DetectorSize.Vertical];

                uint res = SDKInit.SDKInstance.GetAcquiredFloatData(array, (uint)array.Length);

                Console.WriteLine(res == SDK.DRV_SUCCESS ? "Success!" : "Failure!");
                Console.ReadKey();

            }

            
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
