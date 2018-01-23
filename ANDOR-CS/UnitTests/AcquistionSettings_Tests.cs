﻿using System;
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
    public class AcquistionSettings_Tests
    {
        private Camera camera;

        public static void Main()
        {

            var cam = new Camera();
            var setts = cam.GetAcquisitionSettingsTemplate();
            setts.SetImageArea(new DataStructures.Rectangle(new DataStructures.Point2D(1, 1), 128, 256));
            setts.SetExposureTime(123.0f);
            setts.SetVSSpeed(0);
            setts.SetAcquisitionMode(Enums.AcquisitionMode.SingleScan);
            var setts2 = cam.GetAcquisitionSettingsTemplate();
            var sb = new StringBuilder();

            using (var xml = XmlWriter.Create(sb, new XmlWriterSettings() { Indent = true, NewLineOnAttributes = true }))
                setts.WriteXml(xml);


            using (var xml = XmlReader.Create(new System.IO.StringReader(sb.ToString()), new XmlReaderSettings()))
                setts2.ReadXml(xml);

            setts.Dispose();
            setts2.Dispose();
            cam.Dispose();
            Console.WriteLine(sb.ToString());
            Console.ReadKey();
        }
        [TestInitialize]
        public void Initiazlie()
        {
            camera = new Camera();
        }
        [TestCleanup]
        public void Cleanup()
        {
            camera.Dispose();
        }
        [TestMethod]
        public void AcquisitionSettings_Serialize_Deserialize()
        {
            var settings_output = new AcquisitionSettings();
            using (var str = new StreamWriter("UnitTests/AcquistionSettings_Serialize_Deserizalie.xml"))
                settings_output.Serialize(str.BaseStream);

            var settings_input = new AcquisitionSettings(camera);

            var publicProps = typeof(AcquisitionSettings)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod != null);

            var initialVals = publicProps.Select(p => p.GetValue(settings_output)).ToArray();
            var intermVals = publicProps.Select(p => p.GetValue(settings_input)).ToArray();

            CollectionAssert.AreNotEquivalent(initialVals, intermVals);

            using (var str = new StreamReader("UnitTests/AcquistionSettings_Serialize_Deserizalie.xml"))
                settings_input.Deserialize(str.BaseStream);

            var finalVals = publicProps.Select(p => p.GetValue(settings_input)).ToArray();

            CollectionAssert.AreEquivalent(initialVals, finalVals);

            settings_input.Dispose();
            settings_output.Dispose();
        }
    }
}
    