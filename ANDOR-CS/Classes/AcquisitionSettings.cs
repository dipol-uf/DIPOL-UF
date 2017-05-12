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
        private Camera camera = null;


        /// <summary>
        /// Stores the value of currently set vertical speed
        /// </summary>
        public float? VSSpeed
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Stores the index of currently set Analogue-Digital Converter and its bit depth.
        /// </summary>
        public Tuple<int, int> ADConverter
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Stores the value of currently set vertical clock voltage amplitude
        /// </summary>
        public VSAmplitude? VSAmplitude
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Stores type of currentlt set Amplifier
        /// </summary>
        public Tuple<string, OutputAmplification, int> Amplifier
        {
            get;
            private set;
        } = null;



        /// <summary>
        /// Constructor adds reference to parent <see cref="Camera"/> object.
        /// </summary>
        /// <param name="camera">Parent object. Camera, to which settings should be applied.</param>
        internal AcquisitionSettings(Camera camera)
        {
            this.camera = camera;
        }



        /// <summary>
        /// Checks camera parent <see cref="Camera"/>. If it is not initialized or active, throws exception.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="AndorSDKException"/>
        private void CheckCamera(bool requiresActive = true)
        {
            // Checks if camera object is null
            if (camera == null)
                throw new NullReferenceException("Parent camera is null.");

            // Checks if camera is initialized
            if (!camera.IsInitialized)
                throw new AndorSDKException("Camera is not properly initialized.", null);

            // If speed index is invalid
            if (requiresActive && !camera.IsActive)
                throw new AndorSDKException("Camera is not currently active.", null);
        }

        

        /// <summary>
        /// Tries to set vertical speed. 
        /// Camera may not be active.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <param name="speedIndex">Index of available speed that corresponds to VSpeed listed in <see cref="Camera.Properties"/>.VSSpeeds</param>
        public void SetVSSpeed(int speedIndex)
        {
            // Checks if Camera is OK
            CheckCamera(false);

            // Checks if camera actually supports changing vertical readout speed
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalReadoutSpeed))
            {
                // Available speeds max index
                int length = camera.Properties.VSSpeeds.Length;

                // If speed index is invalid
                if (speedIndex < 0 || speedIndex >= length)
                    throw new ArgumentOutOfRangeException($"{nameof(speedIndex)} is out of range (should be in [{0},  {length-1}]).");

                // If success, updates VSSpeed field and 
                VSSpeed = camera.Properties.VSSpeeds[speedIndex];
                // returns success
                
            }
            else
                throw new NotSupportedException("Camera does not support vertical readout speed control.");

        }

        /// <summary>
        /// Tries to set vertical speed to fastest recommended speed. 
        /// Requires camera to be active.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        public void SetVSSpeed()
        {
            // Checks if Camera is OK
            CheckCamera(true);

            // Checks if camera actually supports changing vertical readout speed
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalReadoutSpeed))
            {
                // Stores retrieved recommended vertical speed
                int speedIndex = -1;
                float speedVal = 0;

                uint result = SDKInit.SDKInstance.GetFastestRecommendedVSSpeed(ref speedIndex, ref speedVal);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetFastestRecommendedVSSpeed));

                // Available speeds max index
                int length = camera.Properties.VSSpeeds.Length;

                // If speed index is invalid
                if (speedIndex < 0 || speedIndex >= length)
                    throw new ArgumentOutOfRangeException($"Fastest recommended vertical speed index({nameof(speedIndex)}) " +
                        $"is out of range (should be in [{0},  {length - 1}]).");

                // Calls overloaded version of current method with obtained speedIndex as argument
                SetVSSpeed(speedIndex);

            }
            else
                throw new NotSupportedException("Camera does not support vertical readout speed control.");
            
        }

        /// <summary>
        /// Sets the vertical clock voltage amplitude (if camera supports it).
        /// Camera may be not active.
        /// </summary>
        /// <param name="amplitude">New amplitude </param>
        public void SetVSAmplitude(VSAmplitude amplitude)
        {
            // Checks if Camera is OK
            CheckCamera(false);

            // Checks if Camera supports vertical clock voltage amplitude changes
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalClockVoltage))
            {
                VSAmplitude = amplitude;
            }
            else
                throw new NotSupportedException("Camera does not support vertical clock voltage amplitude control.");
        }
        
        /// <summary>
        /// Sets Analogue-Digital converter.
        /// Does not require camera to be active
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="converterIndex"></param>
        public void SetADConverter(int converterIndex)
        {
            // Checks if Camera is OK
            CheckCamera(false);

            if (converterIndex < 0 || converterIndex >= camera.Properties.ADConverters.Length)
                throw new ArgumentOutOfRangeException($"AD converter index {converterIndex} if out of range " +
                    $"(should be in [{0}, {camera.Properties.ADConverters.Length - 1}]).");

            ADConverter = new Tuple<int, int>(converterIndex, camera.Properties.ADConverters[converterIndex]);

        }

        /// <summary>
        /// Sets output amplifier. 
        /// Does not require camera to be active.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="amplifier"></param>
        public void SetOutputAmplifier(OutputAmplification amplifier)
        {
            // Checks camera object
            CheckCamera(false);

            // Queries available amplifiers, looking for the one, which type mathces input parameter
            var query = from amp
                        in camera.Properties.Amplifiers
                        where amp.Item2 == amplifier
                        select amp;

            // If no mathces found, throws ana excepetion
            if (query.Count() == 0)
                throw new ArgumentOutOfRangeException($"Provided amplifier i sout of range " +
                    $"{(Enum.IsDefined(typeof(OutputAmplification), amplifier) ? Enum.GetName(typeof(OutputAmplification), amplifier) : "Unknown")}.");

            // Otherwise, assigns name and type of the amplifier 
            //Amplifier = query.Select((x) => new Tuple<string, OutputAmplification>(x.Item1, x.Item2)).First();

        }


        public void SetHSSpeed()
        {
            CheckCamera(true);

            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed))
            {
               // if(ADConverter == null || Amplifier == null)
            }
            else
                throw new NotSupportedException("Camera does not support horizontal readout speed controls");
       
        }
    }
}
