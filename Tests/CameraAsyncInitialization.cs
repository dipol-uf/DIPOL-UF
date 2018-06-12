using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class CameraAsyncInitialization
    {
        [TestInitialize]
        public void Initialize()
        {
            Assert.AreNotEqual(Camera.GetNumberOfCameras(), 0);
        }

        [TestMethod]
#if X86
        [DeploymentItem("atmcd32d.dll")]
#endif
#if X64
        [DeploymentItem("atmcd64d.dll")]
#endif
        public void Test_CameraAsyncCreation()
        {
            var cam = CameraBase.CreateCameraAsync<Camera>().Result;
            Assert.IsNotNull(cam);
            cam?.Dispose();
            Assert.IsTrue(cam?.IsDisposed ?? false);
        }

       

    }
}
