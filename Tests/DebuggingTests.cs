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
            public Type Get() => GetType();
        }

        private class B : A
        {
        }

        [TestMethod]
        public void Test()
        {
          
        }
    }
}
