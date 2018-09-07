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
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS.Exceptions;
using DIPOL_Remote.Classes;
using DIPOL_Remote.Faults;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class RemoteCameraTests
    {
        private DipolHost host;
        private DipolClient client;
       
        [TestInitialize]
        public void Initialize()
        {
            host = new DipolHost();
            host.Host();
            

            client = new DipolClient("localhost");
            client.Connect();
        }

        [TestCleanup]
        public void Destroy()
        {
            client.Disconnect();
            client.Dispose();

            host.Dispose();
        }

        [TestMethod]
        public void Test_IsConnected()
        {
            Assert.IsNotNull(client.Remote);
            Assert.IsTrue(client.GetNumberOfCameras() >= 0);
        }

        [TestMethod]
        public void Test_ctor()
        {
            Assert.ThrowsException<ArgumentNullException>(() => RemoteCamera.Create());
            Assert.ThrowsException<ArgumentException>(() => RemoteCamera.Create(0, 5));

            Assert.ThrowsExceptionAsync<ArgumentNullException>(() => RemoteCamera.CreateAsync()).Wait();
            Assert.ThrowsExceptionAsync<ArgumentException>(() => RemoteCamera.CreateAsync(0, 5)).Wait();

        }

        [TestMethod]
        public void Test_CreateRemoteCamera_NoActualCamera()
        {
            Assert.ThrowsException<FaultException<AndorSDKServiceException>>(() => RemoteCamera.Create(0, client));
            Assert.ThrowsExceptionAsync<AndorSdkException>(() => RemoteCamera.CreateAsync(0, client)).Wait();
        }
    }
}
