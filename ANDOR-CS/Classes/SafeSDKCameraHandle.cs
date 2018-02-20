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
using System.Runtime.ConstrainedExecution;

#if X86
using AndorSDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using AndorSDK = ATMCD64CS.AndorSDK;
#endif


namespace ANDOR_CS.Classes
{
    /// <inheritdoc />
    /// <summary>
    /// A safe handle class based on <see cref="T:System.Runtime.InteropServices.SafeHandle" /> class that uses internal <see cref="T:System.IntPtr" /> handle to store
    /// intrinsic AndorSDK camera handle/pointer.
    /// </summary>
    public class SafeSdkCameraHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// A property with getter that returns stored raw Andor SDK camera handle
        /// </summary>
        public int SdkPtr => handle.ToInt32();

        /// <inheritdoc />
        /// <summary>
        /// A constructor. Calls base constructor with ownsHandle = true
        /// </summary>
        /// <param name="handle"></param>
        public SafeSdkCameraHandle(int handle)
            : base(true) => SetHandle((IntPtr)handle);

        /// <inheritdoc />
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
            if (AndorSdkInitialization.SDKInstance == null)
                return false;

            // Variable holds return codes of some Andor SDK methods
            var cameraHandle = 0;
            //// Used to check the currently active camera. 
            
            return AndorSdkInitialization.CallWithoutHandle(() =>
            {
                var result = AndorSdkInitialization.SDKInstance.GetCurrentCamera(ref cameraHandle);
                if (result != AndorSDK.DRV_SUCCESS)
                    return result;

                // Frees camera handles
                result = AndorSdkInitialization.SDKInstance.ShutDown();

                return result;
            }) == AndorSDK.DRV_SUCCESS;
           
        }

    }
}
