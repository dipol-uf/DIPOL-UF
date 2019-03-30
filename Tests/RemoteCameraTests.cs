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

#define HOST_IN_PROCESS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using DIPOL_Remote;
using DIPOL_Remote.Remote;
using NUnit.Framework;

#if !HOST_IN_PROCESS
using System.Diagnostics;
using System.IO;
using Switch = ANDOR_CS.Enums.Switch;
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

          _client = DipolClient.Create(_hostUri);
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
                Arguments = $@"{_hostUri.AbsoluteUri} -l",
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

        [Test]
        public async Task Test_CameraMethods()
        {
           var n = _client.GetNumberOfCameras();

           var camList = await Task.Run(async () =>
           {
               var cams = new Task<RemoteCamera>[n];

               for (var i = 0; i < n; i++)
                   cams[i] = RemoteCamera.CreateAsync(i, _client);
               return await Task.WhenAll(cams);
           });

            
            await Task.WhenAll(camList.Select(async cam =>
            {
                Assert.That(cam.GetStatus, Is.EqualTo(CameraStatus.Idle));
                Assert.That(cam.GetCurrentTemperature, Throws.Nothing);
                Assert.That(() => cam.SetTemperature(0), Throws.Nothing);

                Assert.That(
                    () => cam.ShutterControl(ShutterMode.PermanentlyOpen, ShutterMode.PermanentlyOpen),
                    Throws.Nothing);

                await Task.Delay(50);

                Assert.That(cam.Shutter.Internal, Is.EqualTo(ShutterMode.PermanentlyOpen));
                Assert.That(cam.Shutter.External, Is.EqualTo(ShutterMode.PermanentlyOpen));

                Assert.That(async () =>
                {
                    var result = false;
                    cam.TemperatureStatusChecked += (sender, e) => result = true;
                    cam.TemperatureMonitor(Switch.Enabled, 25);
                    await Task.Delay(100);
                    result = result && cam.IsTemperatureMonitored;
                    cam.TemperatureMonitor(Switch.Disabled);

                    return result;
                }, Is.True);

                Assert.That(() => cam.FanControl(FanMode.FullSpeed), Throws.Nothing);
                Assert.That(cam.FanMode, Is.EqualTo(FanMode.FullSpeed));

                Assert.That(() => cam.CoolerControl(Switch.Enabled), Throws.Nothing);
                Assert.That(cam.CoolerMode, Is.EqualTo(Switch.Enabled));

            }).ToArray());


            foreach (var cam in camList)
                cam.Dispose();
        }

        [Test]
        public async Task Test_AcquisitionSettings()
        {
            using (var camera = await RemoteCamera.CreateAsync(0, _client))
            {
                using (var setts = camera.GetAcquisitionSettingsTemplate())
                {
                    // ReSharper disable AccessToDisposedClosure
                    Assert.Multiple(() =>
                    {
                        Assert.AreEqual(camera, setts.Camera);

#if DEBUG
                        Assert.That(() =>
                        {
                            setts.SupportedSettings();
                            setts.AllowedSettings();
                        }, Throws.Nothing);

                        setts.SetVSSpeed(0);
                        Assert.AreEqual(0, setts.VSSpeed?.Index);
                        Assert.AreEqual(camera.Properties.VSSpeeds[0], setts.VSSpeed?.Speed);

                        setts.SetVSAmplitude(VSAmplitude.Plus3);
                        Assert.AreEqual(VSAmplitude.Plus3, setts.VSAmplitude);

                        var amps = camera.Properties.OutputAmplifiers;
                        var amp = amps.First(x => x.OutputAmplifier == OutputAmplification.ElectronMultiplication);
                        var ampIndex = amps.Select(x => x.OutputAmplifier)
                                           .IndexOf(OutputAmplification.ElectronMultiplication);

                        setts.SetOutputAmplifier(amp.OutputAmplifier);
                        Assert.AreEqual(ampIndex, setts.OutputAmplifier?.Index);
                        Assert.AreEqual(amp.OutputAmplifier, setts.OutputAmplifier?.OutputAmplifier);
                        Assert.AreEqual(amp.Name, setts.OutputAmplifier?.Name);

                        setts.SetADConverter(0);
                        Assert.AreEqual(0, setts.ADConverter?.Index);
                        Assert.AreEqual(camera.Properties.ADConverters[0], setts.ADConverter?.BitDepth);



                        var speed = setts.GetAvailableHSSpeeds().Last();
                        setts.SetHSSpeed(speed.Index);
                        Assert.AreEqual(speed.Index, setts.HSSpeed?.Index);
                        Assert.AreEqual(speed.Speed, setts.HSSpeed?.Speed);

                        var gain = setts.GetAvailablePreAmpGain().Last();
                        setts.SetPreAmpGain(gain.Index);
                        Assert.AreEqual(gain.Index, setts.PreAmpGain?.Index);
                        Assert.AreEqual(gain.Name, setts.PreAmpGain?.Name);

                        setts.SetAcquisitionMode(AcquisitionMode.Kinetic);
                        Assert.AreEqual(AcquisitionMode.Kinetic, setts.AcquisitionMode);
                        
                        setts.SetAccumulateCycle(5, 2f);
                        Assert.AreEqual(5, setts.AccumulateCycle?.Frames);
                        Assert.AreEqual(2f, setts.AccumulateCycle?.Time);

                        setts.SetKineticCycle(10, 10f);
                        Assert.AreEqual(10, setts.KineticCycle?.Frames);
                        Assert.AreEqual(10f, setts.KineticCycle?.Time);
                        
                        setts.SetTriggerMode(TriggerMode.Internal);
                        Assert.AreEqual(TriggerMode.Internal, setts.TriggerMode);

                        setts.SetReadoutMode(ReadMode.FullImage);
                        Assert.AreEqual(ReadMode.FullImage, setts.ReadoutMode);

                        var expTime = 1.2313f;
                        setts.SetExposureTime(expTime);
                        Assert.AreEqual(expTime, setts.ExposureTime);

                        var imgArea = new Rectangle(1, 1, 100, 200);
                        setts.SetImageArea(imgArea);
                        Assert.AreEqual(imgArea, setts.ImageArea);

                        var gains = setts.GetEmGainRange();
                        setts.SetEmCcdGain(gains.Low + 1);
                        Assert.AreEqual(gains.Low + 1, setts.EMCCDGain);
                        
                        #endif
                    });
                    // ReSharper restore AccessToDisposedClosure
                }
            }
        }

        #region Temp tests
        #if DEBUG

        [Test]
        public void Test_CancellationNoCancel()
        {
            var src = 123;
            var res = 0;
            Assert.That(async () => res = await _client.DebugMethodAsync(src, CancellationToken.None),
                Throws.Nothing);
            Assert.That(res, Is.EqualTo(src * src));
        }

        [Test]
        public void Test_CancellationCancelled()
        {
            var src = 123;
            var res = 0;
            Assert.That(async () => 
                    res = await _client.DebugMethodAsync(
                        src, 
                        new CancellationTokenSource(TimeSpan.FromSeconds(1.5)).Token),
                Throws.InstanceOf<TaskCanceledException>());
            Assert.That(res, Is.EqualTo(0));
        }


        #endif
        #endregion
    }
}
