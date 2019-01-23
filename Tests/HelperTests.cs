//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIPOL_UF;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Tests
{

    public class HelperTestData
    {
        public static IEnumerable Test_DoubleEquality_Data
        {
            get
            {
                yield return new TestCaseData(0.6/0.2, 3.0, 2 * MathNet.Numerics.Precision.MachineEpsilon, true);
                yield return new TestCaseData(0.6 / 0.2, 3.0, MathNet.Numerics.Precision.MachineEpsilon, false);
                yield return new TestCaseData(0.1 + 0.2, 0.3, MathNet.Numerics.Precision.MachineEpsilon, true);



            }
        }
    }

    [TestFixture]
    public class HelperTests
    {
        [Test]
        [TestCaseSource(typeof(HelperTestData), nameof(HelperTestData.Test_DoubleEquality_Data))]
        [Parallelizable(ParallelScope.All)]
        public void Test_DoubleEquality(double first, double second, double epsilon, bool areEqual)
        {
            Assert.That(Helper.AreEqual(first, second, epsilon), Is.EqualTo(areEqual));
        }
    }
}
