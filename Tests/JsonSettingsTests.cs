//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SettingsManager;

namespace Tests
{
    [TestFixture]
    public class JsonSettingsTests
    {

        private static string GetSettingsFromFile(string path = "test.json")
        {
            var fPath = Path.GetFullPath(
                Path.IsPathRooted(path)
                    ? path
                    : Path.Combine(TestContext.CurrentContext.TestDirectory, path));
            using (var str = new StreamReader(fPath))
                return str.ReadToEnd();
        }

        [Test]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_Constructor()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assert.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_Get()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");

            void GenerateAssert<T>(string itemName, object compVal) =>
                Assert.That(settings.Get<T>(itemName), 
                    Is.EqualTo(compVal), 
                    $"{nameof(settings.Get)} failed for \"{itemName}\".");


            Assert.Multiple(() =>
            {
                GenerateAssert<int>("item_int", 153);
                GenerateAssert<double>("item_double", 1.23e55);
                GenerateAssert<decimal>("item_decimal", -123.56m);
                GenerateAssert<float>("item_float", 1e10);
                GenerateAssert<string>("item_string", "string");
                GenerateAssert<string>("item_string_empty", "");

                Assert.That(settings.Get<object>("item_null"), Is.Null,
                    $"{nameof(settings.Get)} failed for \"item_null\".");
            });
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_Get_MissingKey()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");

            Assert.Multiple(() =>
            {
                Assert.That(settings.Get<int>("missing_item_int"), Is.EqualTo(0),
                    $"{nameof(settings.Get)} failed for \"missing_item_int\": expected default({nameof(Int32)}).");
                CollectionAssert.AreEqual(new int[] { }, settings.GetArray<int>("missing_item_array_int"),
                    $"{nameof(settings.GetArray)} failed for \"missing_item_array_int\".");
            });
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_GetArray()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 },
                    settings.GetArray<int>("item_array_int"),
                    $"{nameof(settings.GetArray)} failed for \"item_array_int\".");

                CollectionAssert.AreEqual(new object[] { },
                    settings.GetArray<object>("item_array_empty"),
                    $"{nameof(settings.GetArray)} failed for \"item_array_empty\".");
            });
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_GetJson()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");
                
            var item = settings.GetJson("item_nested");

            void GenerateAssert<T>(string itemName, object compVal) =>
                Assert.That(item.Get<T>(itemName),
                    Is.EqualTo(compVal),
                    $"{nameof(settings.Get)} failed for \"{itemName}\".");

            Assert.Multiple(() =>
            {
                Assert.That(item, Is.Not.Null, 
                    $"{nameof(settings.GetJson)} failed for \"item_nested\".");
                Assert.That(item.Count, Is.EqualTo(2),
                    "Wrong size of the \"item_nested\" sub collection.");
                GenerateAssert<int>("nested_int", 153);
                GenerateAssert<decimal>("nested_decimal", 1.53m);
            });
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_HasKey()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");

            Assert.Multiple(() =>
            {
                Assert.That(settings.HasKey("item_empty"), Is.True,
                    "Failed to read \"item_empty\".");
                Assert.That(settings.HasKey("missing_item_empty"), Is.False,
                    "Name of the missing item \"missing_item_empty\" should not be found in the read collection.");
            });
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_GetAsObject()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");

            void GenerateAssert(string itemName, object compVal) =>
                Assert.That(settings.Get(itemName),
                    Is.EqualTo(compVal),
                    $"{nameof(settings.Get)} failed for \"{itemName}\".");

            Assert.Multiple(() =>
            {
                GenerateAssert("item_int", 153L);
                GenerateAssert("item_double", 1.23e55);
                GenerateAssert("item_decimal", -123.56M);
                GenerateAssert("item_float", 1e10f);
                GenerateAssert("item_string", "string");
                GenerateAssert("item_string_empty", "");

                Assert.That(settings.Get("item_null"), Is.Null,
                    $"{nameof(settings.Get)} failed for \"item_null\".");
            });
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_TryGet()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");

            void GenerateAssert<T>(string itemName, object compVal) =>
                Assert.That(settings.TryGet(itemName, out T val) &&
                            compVal.Equals(val),
                    $"{nameof(settings.TryGet)} failed for \"{itemName}\".");

            Assert.Multiple(() =>
            {
                GenerateAssert<long>("item_int", 153L);
                GenerateAssert<double>("item_double", 1.23e55);
                GenerateAssert<double>("item_decimal", -123.56);  
                GenerateAssert<double>("item_float", 1e10);
                GenerateAssert<string>("item_string", "string");
                GenerateAssert<string>("item_string_empty", "");

                Assert.That(settings.TryGet("item_null", out object objectVal) && objectVal == null, Is.True,
                    $"{nameof(settings.TryGet)} fails for \"item_int\".");
                Assert.That(!settings.TryGet("missing_item", out object value) && value == null, Is.True,
                    $"{nameof(settings.TryGet)} fails for \"item_int\".");
            });
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_this()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");

            void GenerateAssert(string itemName, object compVal) =>
                Assert.That(settings[itemName],
                    Is.EqualTo(compVal),
                    $"this[] failed for \"{itemName}\".");

            Assert.Multiple(() =>
            {
                GenerateAssert("item_int", 153L);
                GenerateAssert("item_double", 1.23e55);
                GenerateAssert("item_decimal", -123.56);
                GenerateAssert("item_float", 1e10);
                GenerateAssert("item_string", "string");
                GenerateAssert("item_string_empty", "");
                
                Assert.That(settings["item_null"], Is.Null);
            });
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_Count()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");

            Assert.That(settings.GetJson("item_nested").Count, Is.EqualTo(2),
                $"Incorrect number of elements returned by {settings.Count}.");
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_Set()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");

            var count = settings.Count;
            const string intKey = "test_item_int";
            const int intVal = 42;
            Assert.That(settings.HasKey(intKey), Is.False,
                $"Initial json should not have key \"{intKey}\".");

            settings.Set(intKey, intVal);
            Assert.Multiple(() =>
            {
                Assert.That(settings.Count, Is.EqualTo(count + 1),
                    "Json size should have increased.");
                Assert.That(settings.Get<int>(intKey), Is.EqualTo(intVal),
                    "Added item has wrong value.");
            });

            settings.Set(intKey, intVal + 1);
            Assert.Multiple(() =>
            {
                Assert.That(settings.Count, Is.EqualTo(count + 1),
                    "Json size should have not changed; the item was updated in place.");
                Assert.That(settings.Get<int>(intKey), Is.EqualTo(intVal + 1),
                    "Updated item has wrong value.");
            });
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_ctor()
        {
            var setts = new JsonSettings();
            Assert.That(setts.Count, Is.EqualTo(0),
                $"Wrong number of items in the newly created {nameof(JsonSettings)} class.");
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_Clear()
        {
            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");

            Assume.That(settings["item_int"], Is.EqualTo(153L),
                "Required field is not present in the initial json.");

            settings.Clear("item_int");
            Assert.That(settings["item_int"], Is.Null,
                "Value was not cleared from the json.");
        }

        [Theory]
        [DeployItem("../../../Test inputs/test.json", ForceOverwrite = true)]
        public void Test_TryRemove()
        {
            const string key = "test_key";

            var settings = new JsonSettings(GetSettingsFromFile());
            Assume.That(settings, Is.Not.Null,
                "Settings should be correctly read from JSON file.");

            Assume.That(settings.HasKey(key), Is.False,
                $"Initial json should not have the key \"{key}\".");
            settings.Set(key, key);

            Assert.Multiple(() =>
            {
                Assert.That(settings.Get<string>(key), Is.EqualTo(key), 
                    "Newly added value is incorrect.");
                Assert.That(settings.TryRemove(key), Is.True, 
                    $"Failed to remove item \"{key}\".");
                Assert.That(settings.HasKey(key), Is.False, 
                    $"Removed item \"{key}\" should not be in the json after removal.");
            });

        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_WriteString()
        {
            const long val = 123L;
            const string key = "test";
            var setts = new JsonSettings(new JObject(new JProperty(key, val)));
            var strRep = setts.WriteString();

            var setts2 = new JsonSettings(strRep);

            Assert.Multiple(() =>
            {
                Assert.That(setts2.Count, Is.EqualTo(setts.Count),
                    "Failed to parse back all elements of the initial json.");
                Assert.That(setts2.Get<int>(key), Is.EqualTo(val),
                    "Failed to reconstruct json from the string.");
            });
        }
    }
}
