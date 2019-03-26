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
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace Tests
{
    

    [TestFixture]
    public class RemoteCommunicationTests
    {

        [Theory]
        public void Test_HostProcess()
        {
            var hostConfigString =
                RemoteCommunicationConfigProvider.HostConfig.Get("HostConnectionString", string.Empty);
            Uri uri = null;
            Assume.That(() => Uri.TryCreate(hostConfigString, UriKind.RelativeOrAbsolute, out uri),
                Throws.Nothing);

            // Testing X86 debug config
            var procInfo = new ProcessStartInfo(
                Path.GetFullPath(Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    RemoteCommunicationConfigProvider.HostConfig.Get("HostDirRelativePath", string.Empty),
                    RemoteCommunicationConfigProvider.HostConfig.Get("HostExeName", string.Empty))))
            {
                CreateNoWindow = false,
                ErrorDialog = true,
                WorkingDirectory = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
                    RemoteCommunicationConfigProvider.HostConfig.Get("HostDirRelativePath", string.Empty))),
                Arguments = $@"{uri.AbsoluteUri}",
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            Process proc = null;
            Assert.That(() => proc = Process.Start(procInfo), Throws.Nothing);

            if (proc?.HasExited == false)
                Assert.That(() =>
                {
                    System.Threading.SpinWait.SpinUntil(() => false, TimeSpan.FromMilliseconds(2000));
                    proc.StandardInput.WriteLine("exit");
                    proc.StandardInput.Flush();
                    proc.WaitForExit(10000);
                }, Throws.Nothing);

            Assert.That(proc?.HasExited, Is.True, "Process did not shutdown in time.");

            proc.Dispose();
        }
    }
}
