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
using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;
using DIPOL_Remote.Classes;
using DIPOL_Remote.Faults;
using NUnit.Framework;
using Assert2 = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Tests
{
    [TestFixture]
    public class CameraTests
    {

        [Test]
#if X86
        [DeployItem("../../../../atmcd32d.dll")]
#endif
#if X64
        [DeployItem("../../../../atmcd64d.dll")]
#endif
        public void Test_GetNumberOfCameras_IsNotNegative()
            => Assert.That(Camera.GetNumberOfCameras(), Is.GreaterThanOrEqualTo(0));

        [Theory]
#if X86
        [DeployItem("../../../../atmcd32d.dll")]
#endif
#if X64
        [DeployItem("../../../../atmcd64d.dll")]
#endif
        public void Test_CameraCtor()
        {
            Assume.That(Camera.GetNumberOfCameras(), Is.GreaterThan(0));
            CameraBase cam = new Camera();
            Assert2.IsNotNull(cam, "Camera is null.");
            cam.Dispose();
            Assert2.IsTrue(cam.IsDisposed, $"Failed to dispose [{nameof(cam.IsDisposed)}].");
        }

        //[TestMethod]
#if X86
        [DeployItem("../../../../atmcd32d.dll")]
#endif
#if X64
        [DeployItem("../../../../atmcd64d.dll")]
#endif
        public void Test_CameraBaseCreate_AlwaysThrows()
        {
            Assert2.ThrowsException<NotSupportedException>(() => CameraBase.Create());
        }

        //[TestMethod]
#if X86
        [DeployItem("../../../../atmcd32d.dll")]
#endif
#if X64
        [DeployItem("../../../../atmcd64d.dll")]
#endif
        public void Test_CameraCreate_ThrowsOrCreates()
        {
            var nCam = Camera.GetNumberOfCameras();

            if (nCam == 0)
                Assert2.ThrowsException<AndorSdkException>(() => Camera.Create());
            else
            {
                var cam = Camera.Create();
                Assert2.IsNotNull(cam);
                cam.Dispose();
                Assert2.IsTrue(cam.IsDisposed);
            }
        }


        //[TestMethod]
#if X86
        [DeployItem("../../../../atmcd32d.dll")]
#endif
#if X64
        [DeployItem("../../../../atmcd64d.dll")]
#endif
        public void Test_CameraBaseCreateAsync_AlwaysThrows()
        {
            var except = Assert2.ThrowsException<AggregateException>(() => CameraBase.CreateAsync().Wait());
            Assert2.IsInstanceOfType(except?.InnerException, typeof(NotSupportedException));
        }

        //[TestMethod]
#if X86
        [DeployItem("../../../../atmcd32d.dll")]
#endif
#if X64
        [DeployItem("../../../../atmcd64d.dll")]
#endif
        public void Test_CameraCreateAsync_ThrowsOrCreates()
        {
            var nCam = Camera.GetNumberOfCameras();

            if (nCam == 0)
                Assert2.IsInstanceOfType(
                    Assert2.ThrowsException<AggregateException>(() => Camera.CreateAsync().Wait())?.InnerException,
                    typeof(AndorSdkException));
            else
            {
                var cam = Camera.CreateAsync(0).Result;
                Assert2.IsNotNull(cam);
                cam.Dispose();
                Assert2.IsTrue(cam.IsDisposed);
            }
        }

        //[TestMethod]
#if X86
        [DeployItem("../../../../atmcd32d.dll")]
#endif
#if X64
        [DeployItem("../../../../atmcd64d.dll")]
#endif
        public void Test_RemoteCameraCreate_ThrowsOrCreates()
        {
            using (var client = new DipolClient(@"dipol-2"))
            {
                client.Connect();

                var nCam = client.GetNumberOfCameras();
                if (nCam == 0)
                {

                    Assert2.ThrowsException<System.ServiceModel.FaultException<AndorSDKServiceException>>(() =>
                        RemoteCamera.Create(0, client));

                }
                else
                {
                    var cam = RemoteCamera.Create(0, client);
                    Assert2.IsNotNull(cam);
                    cam.Dispose();
                    Assert2.IsTrue(cam.IsDisposed);
                }

                client.Disconnect();

            }
        }

        //[TestMethod]
#if X86
        [DeployItem("../../../../atmcd32d.dll")]
#endif
#if X64
        [DeployItem("../../../../atmcd64d.dll")]
#endif
        public void Test_RemoteCameraCreateAsync_ThrowsOrCreates()
        {
            using (var client = new DipolClient(@"dipol-2"))
            {
                client.Connect();

                var nCam = client.GetNumberOfCameras();
                if (nCam == 0)
                {
                    var exept = Assert2.ThrowsException<AggregateException>(() => RemoteCamera.CreateAsync(otherParams: client).Wait());
                    Assert2.IsInstanceOfType(exept.InnerException, typeof(AndorSdkException));
                }
                else
                {
                    var cam = RemoteCamera.CreateAsync(otherParams: client).Result;
                    Assert2.IsNotNull(cam);
                    cam.Dispose();
                    Assert2.IsTrue(cam.IsDisposed);
                }

                client.Disconnect();
            }
        }
    }
}
