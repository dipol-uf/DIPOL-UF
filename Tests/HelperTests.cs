using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ANDOR_CS.Enums;
using DIPOL_UF;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Tests
{
    [TestFixture()]
    public class HelperTests
    {
        public enum Test : uint
        {
            [System.ComponentModel.Description("1st")]
            First = 1,
            [System.ComponentModel.Description("2nd")]
            Second = 2,
            [System.ComponentModel.Description("3rd")]
            Third = 4
        }
        [Test]
        public void Test_EnumDescriptors()
        {
            var mode = TemperatureStatus.NotReached;
            
           var res = Helper.GetEnumStringEx(mode).EnumerableToString();
        }
    }
}
