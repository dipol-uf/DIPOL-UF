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
using System.ServiceModel;
using DIPOL_Remote.Classes;
using DIPOL_Remote.Faults;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class DipolClientHostRemoteControlTests
    {
        private DipolHost _host;
        [SetUp]
        public void Initialize()
        {
            _host = new DipolHost();
            _host.Host();
        }

        [TearDown]
        public void Destroy()
        {
            _host.Dispose();
        }

        [Test]
        public void Test_CanConnect()
        {
            using (var client = new DipolClient("localhost"))
            {
                client.Connect();
                Assert.Multiple(() =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    Assert.That(client.Remote, Is.Not.Null);
                    // ReSharper disable once AccessToDisposedClosure
                    Assert.That(client.GetNumberOfCameras, Is.GreaterThanOrEqualTo(0));
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
                    // ReSharper disable once AccessToDisposedClosure
                    Assert.That(client.Remote, Is.Not.Null);
                    // ReSharper disable once AccessToDisposedClosure
                    Assert.That(client.GetNumberOfCameras(), Is.GreaterThanOrEqualTo(0));
                });
                client.Disconnect();
            }
        }

        [Test]
        public void Test_Properties()
        {
            using (var client = new DipolClient("localhost"))
            {
                client.Connect();
                Assert.Multiple(() =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    Assert.That(client.HostAddress, Is.EqualTo("localhost"));
                    // ReSharper disable once AccessToDisposedClosure
                    Assert.That(string.IsNullOrWhiteSpace(client.SessionID), Is.False);
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
                CollectionAssert.AreEqual(new int[] {}, client.ActiveRemoteCameras());
                client.Disconnect();
            }
        }

        [Test]
        public void Test_CreateRemoteCamera_NoActualCamera()
        {
            using (var client = new DipolClient("localhost"))
            {
                client.Connect();
                // ReSharper disable once AccessToDisposedClosure
                Assert.Throws<FaultException<AndorSDKServiceException>>(() => client.CreateRemoteCamera());
                client.Disconnect();
            }
        }
    }
}
