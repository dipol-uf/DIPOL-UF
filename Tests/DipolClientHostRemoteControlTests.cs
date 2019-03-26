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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ANDOR_CS.Classes;
using DIPOL_Remote.Classes;
using NUnit.Framework;
using SettingsManager;

namespace Tests
{
    [TestFixture]
    public class DipolClientHostRemoteControlTests
    {

        private DipolHost _host;
        [SetUp]
        public void Initialize()
        {
            var uriStr = RemoteCommunicationConfigProvider.HostConfig.Get<string>(@"ConnectionString", null);
            if(!Uri.TryCreate(uriStr, UriKind.RelativeOrAbsolute, out var uri))
                throw new InvalidOperationException("No configuration found");
            _host = new DipolHost(uri);
            _host.Host();
        }

        [TearDown]
        public void Destroy()
        {
            _host.Dispose();
        }

        [Test]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public void Test_CanConnect()
        {
            using (var client = new DipolClient("localhost"))
            {
                client.Connect();
                Assert.Multiple(() =>
                {
                    Assert.That(client.Remote, Is.Not.Null,
                        "Remote connection should be established.");
                    Assert.That(client.GetNumberOfCameras, Is.GreaterThanOrEqualTo(0),
                        "Remote instance should report number of cameras greater than or equal to 0.");
                });
                client.Disconnect();
            }

            using (var client = new DipolClient("localhost",
                TimeSpan.FromSeconds(25),
                TimeSpan.FromSeconds(25),
                TimeSpan.FromSeconds(25),
                TimeSpan.FromSeconds(25)))
            {
                client.Connect();
                Assert.Multiple(() =>
                {
                    Assert.That(client.Remote, Is.Not.Null,
                        "Remote connection should be established.");
                    Assert.That(client.GetNumberOfCameras(), Is.GreaterThanOrEqualTo(0),
                        "Remote instance should report number of cameras greater than or equal to 0.");
                });
                client.Disconnect();
            }
        }

        [Test]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]

        public void Test_Properties()
        {
            using (var client = new DipolClient("localhost"))
            {
                client.Connect();
                Assert.Multiple(() =>
                {
                    Assert.That(client.HostAddress, Is.EqualTo("localhost"),
                        $"Local and remote {nameof(client.HostAddress)} should be equal.");

                    Assert.That(string.IsNullOrWhiteSpace(client.SessionID), Is.False,
                        $"{client.SessionID} should be assigned.");
                });
                client.Disconnect();
            }
        }

        [Test]
        public void Test_RemoteActiveCamerasCount()
        {
            using (var client = new DipolClient("localhost"))
            {
                client.Connect();
                CollectionAssert.AreEqual(new int[] {}, client.ActiveRemoteCameras(),
                    "No active cameras should be detected on a tested remote instance.");
                client.Disconnect();
            }
        }

        [Theory]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public void Test_CreateRemoteCamera()
        {
            using (var client = new DipolClient("localhost"))
            {
                client.Connect();
                Assume.That(client.GetNumberOfCameras(), Is.GreaterThan(0),
                    "Camera tests require a camera connected to the computer.");

                CameraBase cam = null;

                Assert.That(() => cam = client.CreateRemoteCamera(), Throws.Nothing,
                    $"Remote camera should be created with {nameof(client.CreateRemoteCamera)} method.");

                cam.Dispose();

                Assert.That(cam.IsDisposed, Is.True,
                    "Camera should be disposed.");
                client.Disconnect();
            }
        }
    }
}
