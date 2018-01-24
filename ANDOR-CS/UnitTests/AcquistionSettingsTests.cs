using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Linq;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ANDOR_CS.Classes;


namespace ANDOR_CS.UnitTests
{
    [TestClass]
    public class AcquistionSettingsTests
    {
        private Camera _camera;

        public static void Main()
        {

            var app = new AcquistionSettingsTests();

            try
            {
                app.Initiazlie();
                app.AcquisitionSettings_Serialize_Deserialize();
            }
            finally
            {
                app.Cleanup();
            }
        }
        [TestInitialize]
        public void Initiazlie()
        {
            _camera = new Camera();
        }
        [TestCleanup]
        public void Cleanup()
        {
            _camera.Dispose();
        }
        [TestMethod]
        public void AcquisitionSettings_Serialize_Deserialize()
        {
            var settingsOutput = _camera.GetAcquisitionSettingsTemplate();

            settingsOutput.SetExposureTime(213f);
            settingsOutput.SetVSSpeed(0);
            settingsOutput.SetAcquisitionMode(Enums.AcquisitionMode.SingleScan);
            settingsOutput.SetImageArea(new DataStructures.Rectangle(1, 1, 256, 128));


            using (var str = new StreamWriter("UnitTests/AcquistionSettings_Serialize_Deserizalie.xml"))
                settingsOutput.Serialize(str.BaseStream);

            var settingsInput = _camera.GetAcquisitionSettingsTemplate();

            var publicProps = typeof(AcquisitionSettings)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod != null);

            var initialVals = publicProps.Select(p => p.GetValue(settingsOutput)).ToArray();
            var intermVals = publicProps.Select(p => p.GetValue(settingsInput)).ToArray();

            CollectionAssert.AreNotEquivalent(initialVals, intermVals);

            using (var str = new StreamReader("UnitTests/AcquistionSettings_Serialize_Deserizalie.xml"))
                settingsInput.Deserialize(str.BaseStream);

            var finalVals = publicProps.Select(p => p.GetValue(settingsInput)).ToArray();

            CollectionAssert.AreEquivalent(initialVals, finalVals);

            settingsInput.Dispose();
            settingsOutput.Dispose();
        }
    }
}
    