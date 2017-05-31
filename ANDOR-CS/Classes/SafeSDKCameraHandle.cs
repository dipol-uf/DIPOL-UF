//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017, Ilia Kosenkov, Tuorla Observatory, Finland

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.ConstrainedExecution;

using SDKInit = ANDOR_CS.Classes.AndorSDKInitialization;

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
