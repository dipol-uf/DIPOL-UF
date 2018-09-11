using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Tests
{
    public class DeployItemAttribute : Attribute, IApplyToTest
    {
        private readonly string _copyFrom;
        private readonly string CopyFrom;

        public DeployItemAttribute(string item) =>
            _copyFrom = item;

        public void ApplyToTest(Test test)
        {
            try
            {
                
                var copyFrom = Path.GetFullPath(
                    Path.IsPathRooted(_copyFrom)
                        ? _copyFrom
                        : Path.Combine(TestContext.CurrentContext.TestDirectory, _copyFrom));

                if (!File.Exists(copyFrom))
                    throw new FileNotFoundException($"File {copyFrom} cannot be found.");

                var shortName = Path.GetFileName(copyFrom);

                var dest = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, shortName));

                if (!File.Exists(dest) ||
                    File.GetLastAccessTimeUtc(copyFrom) > File.GetLastAccessTimeUtc(dest))
                {
                    File.Copy(_copyFrom, dest);
                }
            }
            catch(Exception e)
            {
                test.MakeInvalid($"Failed to prepare test environment. Reason: \"{e.Message}\"");
            }
        }
    }
}
