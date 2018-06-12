using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class AsyncCameraAcquisitionTests
    {
        [TestInitialize]
        public void Initialize()
        {

        }

        [TestMethod]
#if X86
        [DeploymentItem("atmcd32d.dll")]
#endif
#if X64
        [DeploymentItem("atmcd64d.dll")]
#endif
        public void AsyncTest()
        {
            var t = System.Diagnostics.Stopwatch.StartNew();
            var cam = GetCameraAsync();
            cam.Wait();
            t.Stop();
            Assert.IsTrue(t.Elapsed < TimeSpan.FromSeconds(30));
        }

        private static async Task<CameraBase> GetCameraAsync(int index = 0)
        {
            return await Task.Run(() => new Camera(index));
        }

    }
}
