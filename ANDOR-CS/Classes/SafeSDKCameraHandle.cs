using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.ConstrainedExecution;

using SDKInit = ANDOR_CS.AndorSDKInitialization;

namespace ANDOR_CS.Classes
{
    /// <summary>
    /// A safe handle class based on <see cref="System.Runtime.InteropServices.SafeHandle"/> class tha uses internal <see cref="IntPtr"/> handle to store
    /// intrinsic AndorSDK camera handle/pointer.
    /// </summary>
    public class SafeSDKCameraHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// A porperty with getter that returns stored raw Andor SDK camera handle
        /// </summary>
        public int SDKPtr => handle.ToInt32();

        /// <summary>
        /// A constructor. Calls base constructor with ownsHandle = true
        /// </summary>
        /// <param name="handle"></param>
        public SafeSDKCameraHandle(int handle)
            : base(true)
        {
            SetHandle((IntPtr)handle);
        }

        /// <summary>
        /// An overriden ReleaseHandle method that performs all the cleanup procedures. 
        /// Marked for safe execution
        /// </summary>
        /// <returns>true if cleanup finished successfully, false, otherwise, which triggers releaseHandleFailed MDA</returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected override bool ReleaseHandle()
        {
            // This method is only called when this.IsInvalid == false
            // So if for some reason at this point this.IsInvalid == true, then it is definetly an error
            if (IsInvalid)
                return false;

            // If SDK handle is null, resource cannot be freed
            if (SDKInit.SDKInstance == null)
                return false;

            // Variable holds return codes of some Andor SDK methods
            uint result = 0;

            // Used to check the currently active camera. 
            int cameraHandle = 0;

            // Gets currently active camera
            SDKInit.SDKInstance.GetCurrentCamera(ref cameraHandle);
            
            // Checks if currently active camera is not the one that is being disposed
            if (cameraHandle != handle.ToInt32())
            {
                // Force-sets disposing camera as active
                result = SDKInit.SDKInstance.SetCurrentCamera(handle.ToInt32());
                // If it is impossible, then false is returned to indicate failure during resource releasing
                if (result != ATMCD64CS.AndorSDK.DRV_SUCCESS)
                    return false;
            }

            // Frees camera handles
            SDKInit.SDKInstance.ShutDown();

            // If successfully reached this point, all resources are freed. Returns true to indicate success
            return true;
               
        }
    }
}
