using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;

using SDKInit = ANDOR_CS.AndorSDKInitialization;
using SDK = ATMCD64CS.AndorSDK;

using static ANDOR_CS.AndorSDKException;


namespace ANDOR_CS.Classes
{
    /// <summary>
    /// Represents all possible acuisition/camera settings that can be adjusted before taking any images
    /// </summary>
    public class AcquisitionSettings
    {
        /// <summary>
        /// A reference to the parent <see cref="Camera"/> object.
        /// Used to perform checks of capabilities of the current camera.
        /// </summary>
        private Camera source = null;

        /// <summary>
        /// Stores the value of currently set vertical speed
        /// </summary>
        public float? VSSpeed
        {
            get;
            private set;
        } = null;  

        /// <summary>
        /// Constructor adds reference to parent <see cref="Camera"/> object.
        /// </summary>
        /// <param name="source">Parent object. Camera, to which settings should be applied.</param>
        internal AcquisitionSettings(Camera source)
        {
            this.source = source;
        }

        /// <summary>
        /// Tries to set vertical speed
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="speedIndex">Index of available speed that corresponds to VSpeed listed in <see cref="Camera.Properties"/>.VSSpeeds</param>
        /// <returns>true if vertical speed is successfully set, false, otherwsise</returns>
        public bool TrySetVSSpeed(int speedIndex)
        {
            // Checks if camera object is null
            if (source == null)
                throw new NullReferenceException("Parent camera is null.");

            // Makes sure the camera is currently active (otherwise settings will not be applied)
            if (!source.IsActive)
                throw new AndorSDKException("Camera is not currently active.", null);

            // Checks if camera actually supports changing vertical readout speed
            if (source.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalReadoutSpeed))
            {
                // Available speeds max index
                int length = source.Properties.VSSpeeds.Length;

                // If speed index is invalid
                if (speedIndex < 0 || speedIndex >= length)
                    throw new ArgumentOutOfRangeException($"{nameof(speedIndex)} is out of range (should be in [{0},  {length-1}]).");

                // Setting the speed
                uint result = SDKInit.SDKInstance.SetVSSpeed(speedIndex);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.SetVSSpeed));

                // If success, updates VSSpeed field and 
                VSSpeed = source.Properties.VSSpeeds[speedIndex];
                // returns success
                return true;
            }
            // If camera does not support thi setting
            else
                // returns failure
                return false;
        }

        /// <summary>
        /// Tries to set vertical speed to fastest recommended speed.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <returns>true if vertical speed is successfully set, false, otherwsise</returns>
        public bool TrySetVSSpeed()
        {
            // Checks if camera object is null
            if (source == null)
                throw new NullReferenceException("Parent camera is null.");

            // If speed index is invalid
            if (!source.IsActive)
                throw new AndorSDKException("Camera is not currently active.", null);


            // Checks if camera actually supports changing vertical readout speed
            if (source.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalReadoutSpeed))
            {
                // Stores retrieved recommended vertical speed
                int speedIndex = -1;
                float speedVal = 0;

                uint result = SDKInit.SDKInstance.GetFastestRecommendedVSSpeed(ref speedIndex, ref speedVal);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetFastestRecommendedVSSpeed));

                // Available speeds max index
                int length = source.Properties.VSSpeeds.Length;

                // If speed index is invalid
                if (speedIndex < 0 || speedIndex >= length)
                    throw new ArgumentOutOfRangeException($"Fastest recommended vertical speed index({nameof(speedIndex)}) " +
                        $"is out of range (should be in [{0},  {length - 1}]).");

                // Calls overloaded version of current method with obtained speedIndex as argument and returns its return value
                return TrySetVSSpeed(speedIndex);

            }
            // If camera does not support thi setting
            else
                // returns failure
                return false;
        }
    }
}
