using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace FitsTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DeployItemAttribute : NUnitAttribute, IApplyToTest
    {
        public string CopyFrom { get; }
        public string? CopyTo { get; set; }
        public bool ForceOverwrite { get; set; }

        public DeployItemAttribute(string copyFrom) => CopyFrom = copyFrom;

        public void ApplyToTest(Test test)
        {
            static string CollapsePath(string input) =>
                Path.GetFullPath(
                    Path.IsPathRooted(input)
                        ? input
                        : Path.Combine(TestContext.CurrentContext.TestDirectory, input)
                );

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
            catch (Exception e)
            {
                test.MakeInvalid($"Failed to prepare test environment. Reason: \"{e.Message}\".");
            }
        }
    }
}
