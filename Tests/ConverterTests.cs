using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIPOL_UF.Converters;
using NUnit.Framework;

namespace Tests
{
    public static class ConverterTestsData
    {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable Test_CompareToConversion_Data
        {
            get
            {
                yield return new TestCaseData(5, "=5", true);
                yield return new TestCaseData(5.0, "  ==   5", true);
                yield return new TestCaseData(5.0f, "  == 5.0", true);
                yield return new TestCaseData(124.0, "  >= 124", true);
                yield return new TestCaseData(124.0, "  > 124", false);
                yield return new TestCaseData(124.0, "  < -124", false);
                yield return new TestCaseData((byte)128, "  <= 255", true);
                yield return new TestCaseData(128, "  <= 255", true);



            }
        }
    }

    [TestFixture]
    public class ConverterTests
    {
        [Test]
        [TestCaseSource(
            typeof(ConverterTestsData),
            nameof(ConverterTestsData.Test_CompareToConversion_Data))]
        public void Test_CompareToConversion(object src, string comp, bool cond)
            => Assert.That(ConverterImplementations.CompareToConversion(src, comp), Is.EqualTo(cond));
    }
}
