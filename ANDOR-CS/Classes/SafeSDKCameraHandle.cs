using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDKInit = ANDOR_CS.AndorSDKInitialization;

namespace ANDOR_CS.Classes
{
    public class SafeSDKCameraHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public int SDKPtr => handle.ToInt32();

        public SafeSDKCameraHandle(int handle)
            : base(true)
        {
            SetHandle((IntPtr)handle);
        }

        protected override bool ReleaseHandle()
        {
            if (IsInvalid)
                throw new InvalidOperationException("Camera SDK pointer is invalid");

            if (IsClosed)
                throw new ObjectDisposedException($"An instance of {nameof(SafeSDKCameraHandle)} appears to be already disposed.");

            if (SDKInit.SDKInstance == null)
                throw new ArgumentNullException($"A singleton {nameof(ANDOR_CS.AndorSDKInitialization.SDKInstance)} object was disposed.");

            uint result = 0;
            int cameraHandle = 0;

            result = SDKInit.SDKInstance.GetCurrentCamera(ref cameraHandle);
            AndorSDKException.ThrowIfError(result, nameof(SDKInit.SDKInstance.GetCurrentCamera));

            if (cameraHandle != handle.ToInt32())
            {
                result = SDKInit.SDKInstance.SetCurrentCamera(handle.ToInt32());
                AndorSDKException.ThrowIfError(result, nameof(SDKInit.SDKInstance.SetCurrentCamera));
            }

            result = SDKInit.SDKInstance.ShutDown();

            return result == ATMCD64CS.AndorSDK.DRV_SUCCESS;
               
        }
    }
}
