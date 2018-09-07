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
using System.Threading;
using System.Threading.Tasks;
using DIPOL_Remote.Classes;
using DIPOL_Remote.Faults;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class DipolClientHostTests
    {
        private Task _background;
        private readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();
        private DipolHost _host;
        [TestInitialize]
        public void Initialize()
        {
            _background = Task.Run(() =>
            {
                using (_host = new DipolHost())
                {
                    _host.Host();
                    while (!_cancelSource.Token.IsCancellationRequested)
                        Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                }
            }, _cancelSource.Token);
        }

        [TestCleanup]
        public void Destroy()
        {
            _cancelSource.Cancel();
            _background.Wait(TimeSpan.FromSeconds(2));
        }

        [TestMethod]
        public void Test_CanConnect()
        {
            using (var client = new DipolClient("localhost"))
            {
                client.Connect();
                Assert.IsNotNull(client.Remote);
                Assert.IsTrue(client.GetNumberOfCameras() >= 0);
                client.Disconnect();
            }

            using (var client = new DipolClient("localhost",
                TimeSpan.FromSeconds(25),
                TimeSpan.FromSeconds(25),
                TimeSpan.FromSeconds(25),
                TimeSpan.FromSeconds(25)))
            {
                client.Connect();
                Assert.IsNotNull(client.Remote);

                Assert.IsTrue(client.GetNumberOfCameras() >= 0);
                client.Disconnect();
            }
        }

        [TestMethod]
        public void Test_Properties()
        {
            using (var client = new DipolClient("localhost"))
            {
                client.Connect();
                Assert.AreEqual("localhost", client.HostAddress);
                Assert.IsFalse(string.IsNullOrWhiteSpace(client.SessionID));
                client.Disconnect();
            }
        }

        [TestMethod]
        public void Test_RemoteActiveCamerasCount()
        {
            using (var client = new DipolClient("localhost"))
            {
                client.Connect();
                CollectionAssert.AreEqual(new int[] {}, client.ActiveRemoteCameras());
                client.Disconnect();
            }
        }

        [TestMethod]
        public void Test_CreateRemoteCamera_NoActualCamera()
        {
            using (var client = new DipolClient("localhost"))
            {
                client.Connect();
                // ReSharper disable once AccessToDisposedClosure
                Assert.ThrowsException<FaultException<AndorSDKServiceException>>(() => client.CreateRemoteCamera());
                client.Disconnect();
            }
        }
    }
}
