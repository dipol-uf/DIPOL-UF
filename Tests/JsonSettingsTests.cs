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
    //[TestClass]
    [TestFixture]
    public class JsonSettingsTests
    {
        private const string Path1 = @"..\..\..\Tests input\test.Json";
        private const string Path2 = @"..\..\..\Tests\Tests input\test.Json";

        public JsonSettings Settings;

        [SetUp]
        public void Initialize()
        {

            var path1 = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, Path1));
            
            var path2 = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, Path2));

            string str;
            try
            {
                using (var reader = new StreamReader(path1))
                    str = reader.ReadToEnd();
            }
            catch
            {
                using (var reader = new StreamReader(path2))
                    str = reader.ReadToEnd();
            }

            Settings = new JsonSettings(str);
        }

        [Test]
        public void Test_Constructor() =>
            Assert.That(Settings, Is.Not.Null);

        [Test]
        public void Test_Get()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Settings.Get<int>("item_int"), Is.EqualTo(153));
                Assert.That(Settings.Get<int>("item_int"), Is.EqualTo(153));
                Assert.That(Settings.Get<double>("item_double"), Is.EqualTo(1.23e55));
                Assert.That(Settings.Get<decimal>("item_decimal"), Is.EqualTo(-123.56m));
                Assert.That(Settings.Get<double>("item_float"), Is.EqualTo(1e10));
                Assert.That(Settings.Get<string>("item_string"), Is.EqualTo("string"));
                Assert.That(Settings.Get<string>("item_string_empty"), Is.EqualTo(""));
                Assert.That(Settings.Get<object>("item_null"), Is.EqualTo(null));
            });
        }

        [Test]
        public void Test_Get_MissingKey()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Settings.Get<int>("missing_item_int"), Is.EqualTo(0));
                CollectionAssert.AreEqual(new int[] { }, Settings.GetArray<int>("missing_item_array_int"));
            });
        }

        [Test]
        public void Test_GetArray()
        {
            Assert.Multiple(() =>
            {
                CollectionAssert.AreEqual(new[] {1, 2, 3, 4, 5, 6},
                    Settings.GetArray<int>("item_array_int"));

                CollectionAssert.AreEqual(new object[] { },
                    Settings.GetArray<object>("item_array_empty"));
            });
        }

        [Test]
        public void Test_GetJson()
        {
            var item = Settings.GetJson("item_nested");

            Assert.Multiple(() =>
            {
                Assert.That(item.Count, Is.EqualTo(2));
                Assert.That(item.Get<int>("nested_int"), Is.EqualTo(153));
                Assert.That(item.Get<decimal>("nested_decimal"), Is.EqualTo(1.53m));
            });
        }

        [Test]
        public void Test_HasKey()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Settings.HasKey("item_empty"), Is.True);
                Assert.That(Settings.HasKey("missing_item_empty"), Is.False);
            });
        }

        [Test]
        public void Test_GetAsObject()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Settings.Get("item_int"), Is.EqualTo(153L));
                Assert.That(Settings.Get("item_double"), Is.EqualTo(1.23e55));
                Assert.That(Settings.Get("item_decimal"), Is.EqualTo(-123.56));
                Assert.That(Settings.Get("item_float"), Is.EqualTo(1e10));
                Assert.That(Settings.Get("item_string"), Is.EqualTo("string"));
                Assert.That(Settings.Get("item_string_empty"), Is.EqualTo(""));
                Assert.That(Settings.Get("item_null"), Is.EqualTo(null));
            });
        }

        [Test]
        public void Test_TryGet()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Settings.TryGet("item_int", out long intVal) && intVal == 153, Is.True);
                Assert.That(Settings.TryGet("item_double", out double doubleVal) &&
                               Math.Abs(doubleVal - 1.23e55) < double.Epsilon, Is.True);
                Assert.That(Settings.TryGet("item_decimal", out double decimalVal) &&
                               Math.Abs(decimalVal - (-123.56)) < double.Epsilon, Is.True);
                Assert.That(Settings.TryGet("item_float", out double dFloatVal) &&
                               Math.Abs(dFloatVal - 1e10f) < float.Epsilon, Is.True);
                Assert.That(Settings.TryGet("item_string", out string stringVal) && stringVal == "string", Is.True);
                Assert.That(Settings.TryGet("item_string_empty", out string stringValE) && stringValE == "", Is.True);
                Assert.That(Settings.TryGet("item_null", out object objectVal) && objectVal == null, Is.True);
                Assert.That(!Settings.TryGet("missing_item", out object value) && value == null, Is.True);
            });
        }

        [Test]
        public void Test_this()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Settings["item_int"], Is.EqualTo(153L));
                Assert.That(Settings["item_double"], Is.EqualTo(1.23e55));
                Assert.That(Settings["item_decimal"], Is.EqualTo(-123.56));
                Assert.That(Settings["item_float"], Is.EqualTo(1e10));
                Assert.That(Settings["item_string"], Is.EqualTo("string"));
                Assert.That(Settings["item_string_empty"], Is.EqualTo(""));
                Assert.That(Settings["item_null"], Is.EqualTo(null));
            });
        }

        [Test]
        public void Test_Count()
        {
            Assert.That(Settings.GetJson("item_nested").Count, Is.EqualTo(2));
        }

        [Test]
        public void Test_Set()
        {
            var count = Settings.Count;
            const string intKey = "test_item_int";
            const int intVal = 42;
            Assert.That(Settings.HasKey(intKey), Is.False);

            Settings.Set(intKey, intVal);
            Assert.Multiple(() =>
            {
                Assert.That(Settings.Count, Is.EqualTo(count + 1));
                Assert.That(Settings.Get<int>(intKey), Is.EqualTo(intVal));
            });

            Settings.Set(intKey, intVal + 1);
            Assert.Multiple(() =>
            {
                Assert.That(Settings.Count, Is.EqualTo(count + 1));
                Assert.That(Settings.Get<int>(intKey), Is.EqualTo(intVal + 1));
            });
        }

        [Test]
        public void Test_ctor()
        {
            var setts = new JsonSettings();
            Assert.That(setts.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_Clear()
        {
            Assert.That(Settings["item_int"], Is.EqualTo(153L));
            Settings.Clear("item_int");
            Assert.That(Settings["item_int"], Is.EqualTo(null));
        }

        [Test]
        public void Test_TryRemove()
        {
            const string key = "test_key";
            Assert.That(Settings.HasKey(key), Is.False);
            Settings.Set(key, key);
            Assert.Multiple(() =>
            {
                Assert.That(Settings.Get<string>(key), Is.EqualTo(key), "Json value is correct.");
                Assert.That(Settings.TryRemove(key), Is.True, "Json item was removed.");
                Assert.That(Settings.HasKey(key), Is.False, "Json item is removed successfully.");
            });

        }

        [Test]
        public void Test_WriteString()
        {
            var setts = new JsonSettings(new JObject(new JProperty("test", 123L)));
            var strRep = setts.WriteString();

            var setts2 = new JsonSettings(strRep);

            Assert.Multiple(() =>
            {
                Assert.That(setts2.Count, Is.EqualTo(setts.Count));
                Assert.That(setts2.Get<int>("name"), Is.EqualTo(setts.Get<int>("name")));
            });
        }
    }
}
