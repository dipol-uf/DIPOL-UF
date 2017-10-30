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
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new System.Globalization.CultureInfo("en-US");
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
                cam.FanControl(FanMode.LowSpeed);

                var settings = cam.GetAcquisitionSettingsTemplate();


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

                settings.SetAcquisitionMode(AcquisitionMode.Kinetic);

                settings.SetAccumulationCycle(1, 0.2f);

                settings.SetKineticCycle(10, 0.2f);

                var output = settings.ApplySettings(out (float, float, float, int) timing);

                cam.NewImageReceived += ImageReceivedHandler;

                foreach (var o in output)
                    Console.WriteLine(o);


                Console.WriteLine("\r\n" + timing);

                cam.StartAcquistionAsync(new System.Threading.CancellationTokenSource()).Wait();

            }

        }

        private static void ImageReceivedHandler (object sender, ANDOR_CS.Events.NewImageReceivedEventArgs e)
        {

            (sender as CameraBase).AcquiredImages.TryDequeue(out Image image);
            using (var str = new FITSStream(new System.IO.FileStream(String.Format("image_{0:000}.fits", e.First), System.IO.FileMode.Create)))
            {
                FITSKey[] keywords =
                {
                    FITSKey.CreateNew("SIMPLE", FITSKeywordType.Logical, true),
                    FITSKey.CreateNew("BITPIX", FITSKeywordType.Integer, 16),
                    FITSKey.CreateNew("NAXIS", FITSKeywordType.Integer, 2),
                    FITSKey.CreateNew("NAXIS1", FITSKeywordType.Integer, image.Width),
                    FITSKey.CreateNew("NAXIS2", FITSKeywordType.Integer, image.Height),
                    FITSKey.CreateNew("DATE-OBS", FITSKeywordType.String, String.Format("{0:dd'/'MM'/'yy}", e.EventTime.ToUniversalTime())),
                    FITSKey.CreateNew("TIME-OBS", FITSKeywordType.String, String.Format("{0:HH:MM:00}", e.EventTime.ToUniversalTime())),
                    FITSKey.CreateNew("SECONDS", FITSKeywordType.Float, Math.Round(e.EventTime.ToUniversalTime().TimeOfDay.TotalMilliseconds % 60000 / 1000,4)),

                    FITSKey.CreateNew("END", FITSKeywordType.Blank, null)
                };

                var keyUnit = FITSUnit.GenerateFromKeywords(keywords).First();

                var dataUnits = FITSUnit.GenerateFromArray(image.GetBytes(), FITSImageType.Int16);

                str.WriteUnit(keyUnit);
                foreach (var unit in dataUnits)
                    str.WriteUnit(unit);
            }

            //using (var str = new FITSStream(new System.IO.FileStream("test.fits", System.IO.FileMode.Open)))
            //{

            //    List<FITSUnit> keywords = new List<FITSUnit>();
            //    List<FITSUnit> data = new List<FITSUnit>();

            //    while (str.TryReadUnit(out FITSUnit u))
            //    {
            //        if (u.IsKeywords)
            //            keywords.Add(u);
            //        else if (u.IsData)
            //            data.Add(u);
            //    }

            //    var totalKeys = FITSKey.JoinKeywords(keywords.ToArray()).ToList();

            //    var image = FITSUnit.JoinData<Double>(data.ToArray());
            //    var dd = image.ToArray();

            //    int width = totalKeys.First(item => item.Header == "NAXIS1").GetValue<int>();
            //    int height = totalKeys.First(item => item.Header == "NAXIS2").GetValue<int>();

            //    double bScale = totalKeys.First(item => item.Header == "BSCALE").GetValue<int>();
            //    double bZero = totalKeys.First(item => item.Header == "BZERO").GetValue<int>();
            //    FITSImageType type = (FITSImageType)totalKeys.First(item => item.Header == "BITPIX").GetValue<int>();


            //    Image im = new Image(dd, width, height);//.CastTo<Int16, Single>(x => 1.0f * x);

            //    totalKeys.Remove(totalKeys.Where((k) => k.Header == "BITPIX").First());
            //    totalKeys.Insert(1, FITSKey.CreateNew("BITPIX", FITSKeywordType.Integer, -32));
            //    var keysArray = totalKeys.Select((k) => FITSKey.CreateNew(k.Header, k.Type, k.RawValue, k.Comment)).ToArray();
            //    var test = FITSUnit.GenerateFromKeywords(keysArray);
            //    Console.Write(test.Count());

            //    FITSStream.WriteImage(im.CastTo<Double, Int16>(x => (Int16)x), FITSImageType.Int16, "test4.fits");

            //}
        }
    }
}
