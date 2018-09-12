using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DeployItemAttribute : NUnitAttribute, IApplyToTest
    {
        private readonly string _copyFrom;
        private readonly string _copyTo = null;

        public DeployItemAttribute(string from) =>
            _copyFrom = from ?? throw new ArgumentNullException(nameof(from));

        public DeployItemAttribute(string from, string to)
        {
            _copyFrom = from ?? throw new ArgumentNullException(nameof(from));
            _copyTo = to;
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
                var copyFrom = CollapsePath(_copyFrom);

                if (!File.Exists(copyFrom))
                    throw new FileNotFoundException($"File {copyFrom} cannot be found.");

                var copyTo = CollapsePath(_copyTo ?? Path.GetFileName(_copyFrom));

                if (!File.Exists(copyTo) ||
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
