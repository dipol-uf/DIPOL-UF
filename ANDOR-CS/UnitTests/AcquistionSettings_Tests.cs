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
    public class AcquistionSettings_Tests
    {
        private Camera _camera;

        public static void Main()
        {

            var setts = new AcquisitionSettings();
        
            var sb = new StringBuilder();

            using (var xml = XmlWriter.Create(sb, new XmlWriterSettings() { Indent = true, NewLineOnAttributes = true }))
                setts.WriteXml(xml);


            using (var xml = XmlReader.Create(new System.IO.StringReader(sb.ToString()), new XmlReaderSettings()))
                XmlParser.ReadXml(xml);

            setts.Dispose();
            Console.WriteLine(sb.ToString());
            Console.ReadKey();
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
            var settingsOutput = new AcquisitionSettings();
            using (var str = new StreamWriter("UnitTests/AcquistionSettings_Serialize_Deserizalie.xml"))
                settingsOutput.Serialize(str.BaseStream);

            var settingsInput = new AcquisitionSettings(_camera);

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
    