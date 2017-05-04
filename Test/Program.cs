using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDKInit = Test.AndorSDKInitialization;
using SDK = ATMCD64CS.AndorSDK;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Test();
            
            
            Console.ReadKey();
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


                result = SDKInit.SDKInstance.StartAcquisition();
                result = SDKInit.SDKInstance.WaitForAcquisition();

                System.Threading.Thread.Sleep(15000);

                //result = SDKInit.SDKInstance.SaveAsFITS(".\\test.fits", 4);

                int[] array = new int[x * y * n];

                result = SDKInit.SDKInstance.GetAcquiredData(array, (uint)array.Length);

                Console.WriteLine(result == SDK.DRV_SUCCESS ? "Success!" : "Failed!");
                
            }
            
        }

    }
}
