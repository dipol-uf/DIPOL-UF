using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
            
            NewImageImplementation();

            //Console.ReadKey();
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

        private static void NewImageImplementation()
        {
            var model = new DipolImage();
            var viewModel = new DipolImageViewModel(model);
            var wind = new TestWindow {DipolView = {DataContext = viewModel}};



            var app = new Application();
            app.Run(wind);
        }
    }
}
