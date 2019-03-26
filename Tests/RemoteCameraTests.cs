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
using System.ServiceModel;
using ANDOR_CS.Exceptions;
using DIPOL_Remote.Classes;
using DIPOL_Remote.Faults;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class RemoteCameraTests
    {
        private DipolHost _host;
        private DipolClient _client;

       [SetUp]
        public void Initialize()
        {
            var uriStr = RemoteCommunicationConfigProvider.HostConfig.Get<string>(@"ConnectionString", null);
            if (!Uri.TryCreate(uriStr, UriKind.RelativeOrAbsolute, out var uri))
                throw new InvalidOperationException("No configuration found");
            _host = new DipolHost(uri);
            _host.Host();

            _client = new DipolClient(new Uri("net.tcp://localhost:400/DipolHost"));
            _client.Connect();
        }

        [TearDown]
        public void Destroy()
        {
            _client.Disconnect();
            _client.Dispose();

            _host.Dispose();
        }

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
        public void Test_ctor()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentNullException>(() => RemoteCamera.Create());
                Assert.Throws<ArgumentException>(() => RemoteCamera.Create(0, 5));
                      
                Assert.ThrowsAsync<ArgumentNullException>(() => RemoteCamera.CreateAsync());
                Assert.ThrowsAsync<ArgumentException>(() => RemoteCamera.CreateAsync(0, 5));
            });
        }

        [Test]
        public void Test_CreateRemoteCamera_NoActualCamera()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<FaultException<AndorSDKServiceException>>(() => RemoteCamera.Create(0, _client));
                Assert.ThrowsAsync<AndorSdkException>(() => RemoteCamera.CreateAsync(0, _client));
            });
        }
    }
}
