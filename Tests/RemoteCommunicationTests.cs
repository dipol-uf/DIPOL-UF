using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIPOL_Remote.Classes;
using DIPOL_Remote.Faults;
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
        public void Debug()
        {
            using (var client = new DipolClient(@"dipol-2"))
            {
                client.Connect();

                RemoteCamera.CreateAsync(0, client).Wait();
                //System.Threading.Thread.Sleep(TimeSpan.FromSeconds(25));
                //System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));

                client.Disconnect();
            }
        }
    }
}
