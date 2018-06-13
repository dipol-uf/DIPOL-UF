using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;
using DIPOL_Remote.Classes;
using DIPOL_Remote.Faults;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class CameraTests
    {
        [TestMethod]
#if X86
        [DeploymentItem("atmcd32d.dll")]
#endif
#if X64
        [DeploymentItem("atmcd64d.dll")]
#endif
        public void Test_GetNumberOfCameras_ReturnsOne()
            => Assert.AreEqual(Camera.GetNumberOfCameras(), 1);

        [TestMethod]
#if X86
        [DeploymentItem("atmcd32d.dll")]
#endif
#if X64
        [DeploymentItem("atmcd64d.dll")]
#endif
        public void Test_CameraCtor()
        {
            CameraBase cam = new Camera();
            Assert.IsNotNull(cam, "Camera is null.");
            cam.Dispose();
            Assert.IsTrue(cam.IsDisposed, $"Failed to dispose [{nameof(cam.IsDisposed)}].");
        }

        [TestMethod]
#if X86
        [DeploymentItem("atmcd32d.dll")]
#endif
#if X64
        [DeploymentItem("atmcd64d.dll")]
#endif
        public void Test_CameraBaseCreate_AlwaysThrows()
        {
            Assert.ThrowsException<NotSupportedException>(() => CameraBase.Create());
        }

        [TestMethod]
#if X86
        [DeploymentItem("atmcd32d.dll")]
#endif
#if X64
        [DeploymentItem("atmcd64d.dll")]
#endif
        public void Test_CameraCreate_ThrowsOrCreates()
        {
            var nCam = Camera.GetNumberOfCameras();

            if (nCam == 0)
                Assert.ThrowsException<AndorSdkException>(() => Camera.Create());
            else
            {
                var cam = Camera.Create();
                Assert.IsNotNull(cam);
                cam.Dispose();
                Assert.IsTrue(cam.IsDisposed);
            }
        }


        [TestMethod]
#if X86
        [DeploymentItem("atmcd32d.dll")]
#endif
#if X64
        [DeploymentItem("atmcd64d.dll")]
#endif
        public void Test_CameraBaseCreateAsync_AlwaysThrows()
        {
            var except = Assert.ThrowsException<AggregateException>(() => CameraBase.CreateAsync().Wait());
            Assert.IsInstanceOfType(except?.InnerException, typeof(NotSupportedException));
        }

        [TestMethod]
#if X86
        [DeploymentItem("atmcd32d.dll")]
#endif
#if X64
        [DeploymentItem("atmcd64d.dll")]
#endif
        public void Test_CameraCreateAsync_ThrowsOrCreates()
        {
            var nCam = Camera.GetNumberOfCameras();

            if (nCam == 0)
                Assert.IsInstanceOfType(
                    Assert.ThrowsException<AggregateException>(() => Camera.CreateAsync().Wait())?.InnerException,
                    typeof(AndorSdkException));
            else
            {
                var cam = Camera.CreateAsync(0).Result;
                Assert.IsNotNull(cam);
                cam.Dispose();
                Assert.IsTrue(cam.IsDisposed);
            }
        }

        [TestMethod]
#if X86
        [DeploymentItem("atmcd32d.dll")]
#endif
#if X64
        [DeploymentItem("atmcd64d.dll")]
#endif
        public void Test_RemoteCameraCreate_ThrowsOrCreates()
        {
            using (var client = new DipolClient(@"dipol-2"))
            {
                client.Connect();

                int nCam = client.GetNumberOfCameras();
                if (nCam == 0)
                {

                    Assert.ThrowsException<System.ServiceModel.FaultException<AndorSDKServiceException>>(() =>
                        RemoteCamera.Create(0, client));

                }
                else
                {
                    var cam = RemoteCamera.Create(0, client);
                    Assert.IsNotNull(cam);
                    cam.Dispose();
                    Assert.IsTrue(cam.IsDisposed);
                }

                client.Disconnect();

            }
        }

        [TestMethod]
#if X86
        [DeploymentItem("atmcd32d.dll")]
#endif
#if X64
        [DeploymentItem("atmcd64d.dll")]
#endif
        public void Test_RemoteCameraCreateAsync_ThrowsOrCreates()
        {
            using (var client = new DipolClient(@"dipol-2"))
            {
                client.Connect();

                int nCam = client.GetNumberOfCameras();
                if (nCam == 0)
                {
                    try
                    {
                        RemoteCamera.CreateAsync(otherParams: client).Wait();
                    }
                    catch (Exception e)
                    {
                    }
                }

                client.Disconnect();
            }
        }
    }
}
