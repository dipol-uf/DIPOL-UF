using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DipolImage;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Tests
{
    public class DebuggingTests
    {
        [STAThread]
        public static int Main()
        {
            new Debugger();
            return 0;
        }

        public class Debugger
        {
            //public Image TestImageUInt16;

            public Debugger()
            {
                //var initArr = new ushort[256 * 512];
                //for (var i = 0; i < 256; i++)
                //    for (var j = 0; j < 512; j++)
                //        initArr[i * 512 + j] = (ushort)(Math.Pow(i + j, 1.5));

                //TestImageUInt16 = new Image(initArr, 512, 256);
            }

        }
    }
}
