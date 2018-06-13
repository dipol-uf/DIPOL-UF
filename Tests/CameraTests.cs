using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;
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
        {
            Assert.AreEqual(Camera.GetNumberOfCameras(), 1);
        }

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

    }
}
