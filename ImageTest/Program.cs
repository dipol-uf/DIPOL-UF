using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using ANDOR_CS;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;

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
                // ContextSwitchTest();
                FITSTest();
                //Test1();
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

        private static void FITSTest()
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

            }

            using (var str = new FITSStream(new System.IO.FileStream("test.fits", System.IO.FileMode.Open)))
            {

                List<FITSUnit> keywords = new List<FITSUnit>();
                List<FITSUnit> data = new List<FITSUnit>();

                while (str.TryReadUnit(out FITSUnit u))
                {
                    if (u.IsKeywords)
                        keywords.Add(u);
                    else if (u.IsData)
                        data.Add(u);
                }

                var totalKeys = FITSKey.JoinKeywords(keywords.ToArray()).ToList();

                var image = FITSUnit.JoinData<Double>(data.ToArray());
                var dd = image.ToArray();

                int width = totalKeys.First(item => item.Header == "NAXIS1").GetValue<int>();
                int height = totalKeys.First(item => item.Header == "NAXIS2").GetValue<int>();

                double bScale = totalKeys.First(item => item.Header == "BSCALE").GetValue<int>();
                double bZero = totalKeys.First(item => item.Header == "BZERO").GetValue<int>();
                FITSImageType type = (FITSImageType)totalKeys.First(item => item.Header == "BITPIX").GetValue<int>();


                Image im = new Image(dd, width, height);//.CastTo<Int16, Single>(x => 1.0f * x);

                //im.MultiplyByScalar(bScale);
                //im.AddScalar(bZero);

                //FITSKey.CreateNew("TEST", FITSKeywordType.Logical, true, "NOCOMMENT");
                //FITSKey.CreateNew("TEST", FITSKeywordType.Integer, 123456, "NOCOMMENT");
                //FITSKey.CreateNew("TEST", FITSKeywordType.Float, 123456.0f, "NOCOMMENT");
                //FITSKey.CreateNew("TEST", FITSKeywordType.String, "O'HARA ldkjhfkdlhgkdlfhjgkdhsjdghkl123095=8y37iuerhkgjoi3hfldsghldghlkd", "NOCOMMENT");
                //FITSKey.CreateNew("TEST", FITSKeywordType.Complex, new System.Numerics.Complex(123.0, 9999) , "NOCOMMENT12345678901234567890");
                totalKeys.Remove(totalKeys.Where((k) => k.Header == "BITPIX").First());
                totalKeys.Insert(1, FITSKey.CreateNew("BITPIX", FITSKeywordType.Integer, -32));
                var keysArray = totalKeys.Select((k) => FITSKey.CreateNew(k.Header, k.Type, k.RawValue, k.Comment)).ToArray();
                var test = FITSUnit.GenerateFromKeywords(keysArray);
                Console.Write(test.Count());
                //var app = new System.Windows.Application();
                //app.Run(new TestWindow(im));

                //FITSUnit.GenerateFromArray(im.GetBytes(), type = FITSImageType.Double);

                //using (var str2 = new FITSStream(new System.IO.FileStream("test3.fits", System.IO.FileMode.OpenOrCreate)))
                //{
                //    str2.Write(test.First().Data, 0, FITSUnit.UnitSizeInBytes);
                //    foreach (var unit in FITSUnit.GenerateFromArray(im.CastTo<double, Single>((x) => (Single)x).GetBytes(), FITSImageType.Single))
                //        str2.WriteUnit(unit);
                //}

                FITSStream.WriteImage(im.CastTo<Double, Int16>(x => (Int16)x), FITSImageType.Int16, "test4.fits");

            }
        }
    }
}
