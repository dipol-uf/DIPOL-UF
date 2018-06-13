using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ANDOR_CS.Classes;
using DipolImage;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Tests
{
    [TestClass]
    public class DebuggingTests
    {
        private class A
        {
            public static string Get() => "AA";
        }

        private class B : A
        {
            public new static string Get() => "BB";
        }

        [TestMethod]
        public void Test()
        {
            var a = A.Get();
            var b = B.Get();

            Assert.AreNotEqual(a, b);
        }
    }
}
