using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDKInit = ANDOR_CS.AndorSDKInitialization;
using SDK = ATMCD64CS.AndorSDK;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;

namespace ANDOR_CS
{
    class Program
    {
        static void Main(string[] args)
        {
            //throw new AndorSDKException($"TestException in {nameof(Main)}", SDK.DRV_COFERROR);

            //Test();

            Test3();

            
            
           // Console.ReadKey();
        }

        public static void Test()
        {
            using (var cam = new Camera())
            {
                var temp1 = cam.GetCurrentTemperature();
                uint result = 0;
                result = SDKInit.SDKInstance.SetVSSpeed(1);
                // EM-amp
                result = SDKInit.SDKInstance.SetHSSpeed(1, 1);

                // Frame transfer ON
                result = SDKInit.SDKInstance.SetFrameTransferMode(1);
                
                // Single Scan
                result = SDKInit.SDKInstance.SetAcquisitionMode(3);

                int n = 3;
                // 3 images in a series
                result = SDKInit.SDKInstance.SetNumberKinetics(n);

                // No accumulation
                result = SDKInit.SDKInstance.SetNumberAccumulations(1);

                result = SDKInit.SDKInstance.SetExposureTime(1.0f);

                // Image
                result = SDKInit.SDKInstance.SetReadMode(4);

                int x = 0, y = 0;
                result = SDKInit.SDKInstance.GetDetector(ref x, ref y);

                result = SDKInit.SDKInstance.SetImage(1, 1, 1, x, 1, y);

                var now = DateTime.Now;
                result = SDKInit.SDKInstance.StartAcquisition();
                result = SDKInit.SDKInstance.WaitForAcquisition();

                System.Threading.Thread.Sleep(5000);

                //result = SDKInit.SDKInstance.SaveAsSif(".\\test.sif");

                //result = SDKInit.SDKInstance.SaveAsRaw(".\\test.raw", 3);

                //result = SDKInit.SDKInstance.SaveAsFITS(".\\test.fits", 2);

                float[] array = new float[x * y * n];

                result = SDKInit.SDKInstance.GetAcquiredFloatData(array, (uint)array.Length);

                var imgData = GetImage(array.Take(x * y).Select((p) => (double)p).ToArray(), x, y);

                List<GetFITS.Keyword> keys = new List<GetFITS.Keyword>();
                keys.Add(new GetFITS.Keyword("TIME", GetFITS.KeywordType.String, now.ToString("yyyy-MM-dd hh:mm:ss.ffffff"), "Custom keyword"));

                var block = GetFITS.FITSBlock.FITSFromImage(imgData, GetFITS.PixType.FLOAT_32, keys);

                using (var file = GetFITS.FITSFile.CreateNew(new System.IO.FileStream("newfits.fits", System.IO.FileMode.OpenOrCreate)))
                {
                    file.AssignHDUs(block);

                    file.WriteToStream();
                }

                Console.WriteLine(result == SDK.DRV_SUCCESS ? "Success!" : "Failed!");

            }
            
        }

        public static double[,] GetImage(double[] data, int n, int m)
        {
            double[,] result = new double[n, m];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    result[i, j] = data[j + m * i];

            return result;
        }

        public static void Test2()
        {
            SDK.AndorCapabilities caps = default(SDK.AndorCapabilities);

            caps.ulAcqModes = 1 | 8 | 64 | 32 | 2;

            DeviceCpabilities newCaps = new DeviceCpabilities(caps);

            //EnumNames.GetName(typeof(int), newCaps.AcquisitionModes);
            
            foreach (var name in EnumNames.GetName(typeof(AcquisitionMode), newCaps.AcquisitionModes).Skip(1))
                Console.Write(name + " ");

           // Console.ReadKey();
            
        }

        public static void Test3()
        {
            using (Camera cam = new Camera())
            {
                var x = cam.Capabilities;


                Console.WriteLine("\r\nAcquisition Modes:");
                foreach (var val in EnumNames.GetName(typeof(AcquisitionMode), x.AcquisitionModes).Skip(1))
                    Console.WriteLine("\t> " + val);

                Console.WriteLine("\r\nRead Modes:");
                foreach (var val in EnumNames.GetName(typeof(ReadMode), x.ReadModes).Skip(1))
                    Console.WriteLine("\t> " + val);

                Console.WriteLine("\r\nFT Read Modes:");
                foreach (var val in EnumNames.GetName(typeof(ReadMode), x.FTReadModes).Skip(1))
                    Console.WriteLine("\t> " + val);

                Console.WriteLine("\r\nTrigger Modes:");
                foreach (var val in EnumNames.GetName(typeof(TriggerMode), x.TriggerModes).Skip(1))
                    Console.WriteLine("\t> " + val);

                Console.WriteLine("\r\nCamera Type:");
                Console.WriteLine("\t> " + x.CameraType);

                Console.WriteLine("\r\nPixel Modes:");
                foreach (var val in EnumNames.GetName(typeof(PixelMode), x.PixelModes))
                    Console.WriteLine("\t> " + val);

                Console.WriteLine("\r\nSet Functions:");
                foreach (var val in EnumNames.GetName(typeof(SetFunction), x.SetFunctions).Skip(1))
                    Console.WriteLine("\t> " + val);

                Console.WriteLine("\r\nGet Functions:");
                foreach (var val in EnumNames.GetName(typeof(GetFunction), x.GetFunctions).Skip(1))
                    Console.WriteLine("\t> " + val);

                Console.WriteLine("\r\nFeatures:");
                foreach (var val in EnumNames.GetName(typeof(SDKFeatures), x.Features).Skip(1))
                    Console.WriteLine("\t> " + val);

                Console.WriteLine("\r\nEM Gain:");
                foreach (var val in EnumNames.GetName(typeof(EMGain), x.EMGainFeatures).Skip(1))
                    Console.WriteLine("\t> " + val);



            }
        }
    }
}
