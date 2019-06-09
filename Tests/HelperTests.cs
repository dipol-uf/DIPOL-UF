using System;
using ANDOR_CS.Enums;
using DIPOL_UF;
using NUnit.Framework;

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

        [Test]
        public void Test_EnumToString()
        {
            Enum mode = ReadMode.FullImage;
            CollectionAssert.AreEquivalent(mode.GetEnumStringEx(), new[] {"Full image"});

            mode = ReadMode.Unknown;
            CollectionAssert.AreEquivalent(mode.GetEnumStringEx(), new[] { "Unknown" });

            mode = TriggerMode.Internal;
            CollectionAssert.AreEquivalent(mode.GetEnumStringEx(), new[] { "Internal" });

        }
    }
}
