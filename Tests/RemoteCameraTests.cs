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

//#define HOST_IN_PROCESS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DIPOL_Remote.Classes;
using NUnit.Framework;

#if !HOST_IN_PROCESS
using System.Diagnostics;
using System.IO;
#endif

namespace Tests
{
    [TestFixture]
    public class RemoteCameraTests
    {
        private DipolClient _client;

        private Uri _hostUri;

#if HOST_IN_PROCESS
        private DipolHost _host;
        [SetUp]
        public void Initialize()
        {
            var hostConfigString =
                RemoteCommunicationConfigProvider.HostConfig.Get("HostConnectionString", string.Empty);
            if (!Uri.TryCreate(hostConfigString, UriKind.RelativeOrAbsolute, out _hostUri))
                throw new InvalidOperationException("Bad connection string");

          _host = new DipolHost(_hostUri);
          _host.Open();

          _client = new DipolClient(_hostUri);
          _client.Connect();
        }

        [TearDown]
        public void Destroy()
        {
            _client.Disconnect();
            _client.Dispose();
            _host.Dispose();
        }
#else
        private Process _proc;

        [SetUp]
        public void Initialize()
        {
            var hostConfigString =
                RemoteCommunicationConfigProvider.HostConfig.Get("HostConnectionString", string.Empty);
            if (!Uri.TryCreate(hostConfigString, UriKind.RelativeOrAbsolute, out _hostUri))
                throw new InvalidOperationException("Bad connection string");

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
                Arguments = $@"{_hostUri.AbsoluteUri}",
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            _proc = Process.Start(procInfo);

            _client = new DipolClient(_hostUri);
            _client.Connect();

        }

        [TearDown]
        public void Destroy()
        {
            _client.Disconnect();
            _client.Dispose();
            if (_proc?.HasExited == false)
            {
                _proc.StandardInput.WriteLine("exit");
                _proc.WaitForExit(10000);
            }

            if (_proc?.HasExited == false)
            {
                _proc?.Kill();
                throw new InvalidOperationException("Failed to exit process.");
            }

            _proc?.Dispose();
        }
#endif

        [Test]
        public void Test_IsConnected()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_client.Remote, Is.Not.Null);
                Assert.That(_client.GetNumberOfCameras(), Is.GreaterThanOrEqualTo(0));
            });
        }

        [Test]
        public void Test_Create()
        {
            RemoteCamera cam = null;
            Assert.That(() => cam = RemoteCamera.Create(0, _client), Throws.Nothing);
            Assert.That(cam, Is.Not.Null);

            cam?.Dispose();
        }

        [Test]
        public void Test_CreateAsync()
        {
            RemoteCamera cam = null;
            Assert.That(async() => cam = await RemoteCamera.CreateAsync(0, _client).ConfigureAwait(false),
                Throws.Nothing);
            Assert.That(cam, Is.Not.Null);

            cam?.Dispose();
        }

        [Test]
        public void Test_ctor()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(() => RemoteCamera.Create());
                Assert.Throws<ArgumentException>(() => RemoteCamera.Create(0, 5));
                      
                Assert.ThrowsAsync<ArgumentException>(async () => await RemoteCamera.CreateAsync());
                Assert.ThrowsAsync<ArgumentException>(async () => await RemoteCamera.CreateAsync(0, 5));
            });
        }

        [Theory]
        public void Test_CreateMultipleCameras()
        {
            var n = _client.GetNumberOfCameras();
            Assume.That(n, Is.GreaterThanOrEqualTo(1));


            var camList = Task.Run(async () =>
            {
                var cams = new Task<RemoteCamera>[n];

                for (var i = 0; i < n; i++)
                    cams[i] = RemoteCamera.CreateAsync(i, _client);
                return await Task.WhenAll(cams);
            }).GetAwaiter().GetResult();

            CollectionAssert.AllItemsAreNotNull(camList);

            foreach(var cam in camList)
                cam.Dispose();
        }

        [Test]
        public void Test_CameraProperties()
        {
            void Generate<TTarget>(IEnumerable<RemoteCamera> cams, Func<RemoteCamera, TTarget> accessor)
            {
                List<TTarget> result = null;
                Assert.That(() => result = cams.Select(accessor).ToList(), Throws.Nothing);
                CollectionAssert.AllItemsAreNotNull(result);
            }


            var n = _client.GetNumberOfCameras();

            var camList = Task.Run(async () =>
            {
                var cams = new Task<RemoteCamera>[n];

                for (var i = 0; i < n; i++)
                    cams[i] = RemoteCamera.CreateAsync(i, _client);
                return await Task.WhenAll(cams);
            }).GetAwaiter().GetResult();

            Assert.Multiple(() =>
            {
                Generate(camList, x => x.IsTemperatureMonitored);
                Generate(camList, x => x.CameraModel);
                Generate(camList, x => x.SerialNumber);
                Generate(camList, x => x.IsActive);
                Generate(camList, x => x.Properties);
                Generate(camList, x => x.IsInitialized);
                Generate(camList, x => x.FanMode);
                Generate(camList, x => x.CoolerMode);
                Generate(camList, x => x.Capabilities);
                Generate(camList, x => x.IsAcquiring);
                Generate(camList, x => x.Shutter);
                Generate(camList, x => x.Software);
                Generate(camList, x => x.Hardware);
            });

            foreach (var cam in camList)
               cam.Dispose();
        }

    }
}
