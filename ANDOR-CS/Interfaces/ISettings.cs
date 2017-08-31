using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;

namespace ANDOR_CS.Interfaces
{
    public interface ISettings
    {
        int CameraIndex
        { get; }

        /// <summary>
        /// Stores the value of currently set vertical speed
        /// </summary>
        (int Index, float Speed)? VSSpeed
        {
            get;
        // // private set;
        } 

        /// <summary>
        /// Stores the value of currently set horizontal speed
        /// </summary>
        (int Index, float Speed)? HSSpeed
        {
            get;
            // private set;
        }// = null;

        /// <summary>
        /// Stores the index of currently set Analogue-Digital Converter and its bit depth.
        /// </summary>
        (int Index, int BitDepth)? ADConverter
        {
            get;
            // private set;
        }// = null;

        /// <summary>
        /// Stores the value of currently set vertical clock voltage amplitude
        /// </summary>
        VSAmplitude? VSAmplitude
        {
            get;
            // private set;
        }// = null;

        /// <summary>
        /// Stores type of currentlt set Amplifier
        /// </summary>
        (string Name, OutputAmplification Amplifier, int Index)? Amplifier
        {
            get;
            // private set;
        }// = null;

        /// <summary>
        /// Stores type of currently set PreAmp Gain
        /// </summary>
        (int Index, string Name)? PreAmpGain
        {
            get;
            // private set;
        }// = null;

        /// <summary>
        /// Stores currently set acquisition mode
        /// </summary>
        AcquisitionMode? AcquisitionMode
        {
            get;
            // private set;
        }// = null;

        /// <summary>
        /// Stores currently set read mode
        /// </summary>
        ReadMode? ReadMode
        {
            get;
            // private set;
        }

        /// <summary>
        /// Stores currently set trigger mode
        /// </summary>
        TriggerMode? TriggerMode
        {
            get;
            // private set;
        }

        /// <summary>
        /// Stores exposure time
        /// </summary>
        float? ExposureTime
        {
            get;
            // private set;
        }// = null;

        /// <summary>
        /// Stoers seleced image area - part of the CCD from where data should be collected
        /// </summary>
        Rectangle? ImageArea
        {
            get;
            // private set;
        }// = null;

        (int Frames, float Time)? AccumulateCycle
        {
            get;
            // private set;
        }// = null;

        (int Frames, float Time)? KineticCycle
        {
            get;
            // private set;
        }// = null;

        int? EMCCDGain
        {
            get;
            // private set;
        }

        List<(string Option, bool Success, uint ReturnCode)> ApplySettings(
            out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing);


        /// <summary>
        /// Tries to set vertical speed. 
        /// Camera may not be active.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <param name="speedIndex">Index of available speed that corresponds to VSpeed listed in <see cref="Camera.Properties"/>.VSSpeeds</param>
        void SetVSSpeed(int speedIndex);

        /// <summary>
        /// Tries to set vertical speed to fastest recommended speed. 
        /// Requires camera to be active.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        void SetVSSpeed();

        /// <summary>
        /// Sets the vertical clock voltage amplitude (if camera supports it).
        /// Camera may be not active.
        /// </summary>
        /// <param name="amplitude">New amplitude </param>
        void SetVSAmplitude(VSAmplitude amplitude);

        /// <summary>
        /// Sets Analogue-Digital converter.
        /// Does not require camera to be active
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="converterIndex"></param>
        void SetADConverter(int converterIndex);

        /// <summary>
        /// Sets output amplifier. 
        /// Does not require camera to be active.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="amplifier"></param>
        void SetOutputAmplifier(OutputAmplification amplifier);

        /// <summary>
        /// Returns a collection of available Horizonal Readout Speeds for currently selected Amplifier and AD Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/> and <see cref="AcquisitionSettings.Amplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/> and <see cref="AcquisitionSettings.SetOutputAmplifier(OutputAmplification)"/>
        /// before calling this method.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <returns>An enumerable collection of speed indexes and respective speed values available.</returns>
        IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds();

        /// <summary>
        /// Sets Horizontal Readout Speed for currently selected Amplifier and AD Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/> and <see cref="AcquisitionSettings.Amplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/> and <see cref="AcquisitionSettings.SetOutputAmplifier(OutputAmplification)"/>
        /// before calling this method.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <param name="speedIndex">Index of horizontal speed</param>
        void SetHSSpeed(int speedIndex);

        /// <summary>
        /// Returns a collection of available PreAmp gains for currently selected HSSpeed, Amplifier, Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/>, <see cref="AcquisitionSettings.HSSpeed"/>
        /// and <see cref="AcquisitionSettings.Amplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/>, <see cref="AcquisitionSettings.SetHSSpeed(int)"/>
        /// and <see cref="AcquisitionSettings.SetOutputAmplifier(OutputAmplification)"/>.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="NotSupportedException"/>
        /// <returns>Available PreAmp gains</returns>
        IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain();

        /// <summary>
        /// Sets PreAmp gain for currently selected HSSpeed, Amplifier, Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/>, <see cref="AcquisitionSettings.HSSpeed"/>
        /// and <see cref="AcquisitionSettings.Amplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/>, <see cref="AcquisitionSettings.SetHSSpeed(int)"/>
        /// and <see cref="AcquisitionSettings.SetOutputAmplifier(OutputAmplification)"/>.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="gainIndex">Index of gain</param>
        void SetPreAmpGain(int gainIndex);

        /// <summary>
        /// Sets acquisition mode. 
        /// Camera may be inactive.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="mode">Acquisition mode</param>
        void SetAcquisitionMode(AcquisitionMode mode);

        /// <summary>
        /// Sets trigger mode. 
        /// Camera may be inactive.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="mode">Trigger mode</param>
        void SetTriggerMode(TriggerMode mode);

        /// <summary>
        /// Sets read mode.
        /// Camera may be inactive.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="mode">Read mode</param>
        void SetReadoutMode(ReadMode mode);

        /// <summary>
        /// Sets exposure time.
        /// Camera may be inactive.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="time">Exposure time</param>
        void SetExposureTime(float time);

        /// <summary>
        /// Sets image area.
        /// Camera may be inactive.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="area">Image rectangle</param>
        void SetImageArea(Rectangle area);

        void SetEMCCDGain(int gain);

        void SetAccumulationCycle(int number, float time);

        void SetKineticCycle(int number, float time);
        
    }
}
