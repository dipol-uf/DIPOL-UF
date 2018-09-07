using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            public static string Get2() => Get();
        }

        private class B : A
        {
            public new static string Get() => "BB";
            public new static string Get2() => Get();

        }

        [TestMethod]
        public void Test()
        {
            var a = A.Get();
            var b = B.Get();

            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void Test2()
        {
            var a = A.Get2();
            var b = B.Get2();

            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void Test_Async()
        {
            async Task Run(int i)
            {
                await Task.Delay(1500);
                Debug.WriteLine(i);
            }

            async Task Run2(int n)
            {
                var tasks = new Task[n];
                for (var j = 0; j < n; j++)
                    tasks[j] = Run(j);

                await Task.WhenAll(tasks);
            }

            Run2(100).Wait();

        }
    }
}
