using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        [Flags]
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

            var res = mode.GetEnumStringEx().EnumerableToString();
        }

        [Test]
        public void Test_TupleDescriptors()
        {
            var x = (A: Test.First | Test.Third, B: new[]{6, 10}, C: (true, false));


            var str = x.GetValueTupleString();
        }
    }
}
