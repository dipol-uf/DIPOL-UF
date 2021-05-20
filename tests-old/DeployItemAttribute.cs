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
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DeployItemAttribute : NUnitAttribute, IApplyToTest
    {
        public string CopyFrom { get; }
        public string CopyTo { get; set; } = null;
        public bool ForceOverwrite { get; set; } = false;

        public DeployItemAttribute(string copyFrom)
        {
            CopyFrom = copyFrom;
        }

        public void ApplyToTest(Test test)
        {

            string CollapsePath(string input) =>
                Path.GetFullPath(
                    Path.IsPathRooted(input)
                        ? input
                        : Path.Combine(TestContext.CurrentContext.TestDirectory, input));

            try
            {
                var copyFrom = CollapsePath(CopyFrom);

                if (!File.Exists(copyFrom))
                    throw new FileNotFoundException($"File {copyFrom} cannot be found.");

                var copyTo = CollapsePath(CopyTo ?? Path.GetFileName(CopyFrom));

                if (ForceOverwrite ||
                    !File.Exists(copyTo) ||
                    File.GetLastAccessTimeUtc(copyFrom) > File.GetLastAccessTimeUtc(copyTo))
                {
                    File.Copy(copyFrom, copyTo, true);
                }
            }
            catch(Exception e)
            {
                test.MakeInvalid($"Failed to prepare test environment. Reason: \"{e.Message}\".");
            }
        }
    }
}
