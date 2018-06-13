using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIPOL_Remote.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class RemoteCommunicationTests
    {
        [TestMethod]
#if X86
        [DeploymentItem("atmcd32d.dll")]
#endif
#if X64
        [DeploymentItem("atmcd64d.dll")]
#endif
        public void Test_CameraCreationRequest()
        {
            using(var client = new DipolClient(@"dipol-2"))
            {
                var cam = RemoteCamera.Create(0, client);
                Assert.IsNotNull(cam);
                cam.Dispose();
                Assert.IsTrue(cam.IsDisposed);
                client.Disconnect();
            }
        }
    }
}
