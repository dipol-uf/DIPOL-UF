//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.


using System;
using System.ServiceModel;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DIPOL_Remote.Callback;
using DIPOL_Remote.Enums;
using DIPOL_Remote.Faults;

namespace DIPOL_Remote.Remote
{
    /// <summary>
    /// Interface used to communicate with DIPOL service.
    /// </summary>
    [ServiceContract(
        SessionMode = SessionMode.Required,
        CallbackContract = typeof(IRemoteCallback))]
    [ServiceKnownType(typeof(Version))]
    [ServiceKnownType(typeof(CameraProperties))]
    [ServiceKnownType(typeof(DeviceCapabilities))]
    [ServiceKnownType(typeof(Size))]
    [ServiceKnownType(typeof(OutputAmplification))]
    [ServiceKnownType(typeof(FanMode))]
    [ServiceKnownType(typeof(Switch))]
    [ServiceKnownType(typeof(AcquisitionMode))]
    [ServiceKnownType(typeof(ReadMode))]
    [ServiceKnownType(typeof(TriggerMode))]
    [ServiceKnownType(typeof(CameraType))]
    [ServiceKnownType(typeof(PixelMode))]
    [ServiceKnownType(typeof(SetFunction))]
    [ServiceKnownType(typeof(GetFunction))]
    [ServiceKnownType(typeof(SdkFeatures))]
    [ServiceKnownType(typeof(EmGain))]
    [ServiceKnownType(typeof(CameraStatus))]
    [ServiceKnownType(typeof(ShutterMode))]
    [ServiceKnownType(typeof(TtlShutterSignal))]
    [ServiceKnownType(typeof(TemperatureStatus))]
    [ServiceKnownType(typeof(AcquisitionEventType))]
    [ServiceKnownType(typeof(AcquisitionStatusEventArgs))]
    [ServiceKnownType(typeof(TemperatureStatusEventArgs))]
    [ServiceKnownType(typeof(NewImageReceivedEventArgs))]
    [ServiceKnownType(typeof(DipolImage.Image))]
    [ServiceKnownType(typeof(TypeCode))]
    [ServiceKnownType(typeof(RemoteCancellationToken))]
    public interface IRemoteControl
    {
        /// <summary>
        /// Unique ID of the session 
        /// </summary>
        string SessionID
        {
            [OperationContract(IsOneWay = false)]
            get;
        }

        /// <summary>
        /// Entry point of the connection
        /// </summary>
        [OperationContract(IsInitiating = true, IsOneWay = false)]
        [FaultContract(typeof(ServiceFault))]
        void Connect();
        /// <summary>
        /// Exit point of the connection. Frees resources
        /// </summary>
        [OperationContract(IsTerminating = true, IsOneWay = false)]
        [FaultContract(typeof(ServiceFault))]
        void Disconnect();

        /// <summary>
        /// Returns number of available cameras
        /// </summary>
        /// <returns></returns>
        [OperationContract(IsOneWay = false)]
        [FaultContract(typeof(AndorSdkFault))]
        [FaultContract(typeof(ServiceFault))]
        int GetNumberOfCameras();

        [OperationContract(IsOneWay = false)]
        [FaultContract(typeof(AndorSdkFault))]
        [FaultContract(typeof(ServiceFault))]
        void CreateCamera(int camIndex = 0);

        [OperationContract(IsOneWay = false)]
        [FaultContract(typeof(ServiceFault))]
        void RemoveCamera(int camIndex);

        [OperationContract(IsOneWay = false)]
        int[] GetCamerasInUse();

        [OperationContract(IsOneWay = false)]
        [FaultContract(typeof(ServiceFault))]
        string CreateSettings(int camIndex);

        [OperationContract(IsOneWay = false)]
        void RemoveSettings(string settingsID);



        [OperationContract(IsOneWay = false)]
        string GetCameraModel(int camIndex);

        [OperationContract(IsOneWay = false)]
        bool GetIsActive(int camIndex);

        [OperationContract(IsOneWay = false)]
        string GetSerialNumber(int camIndex);

        [OperationContract(IsOneWay = false)]
        CameraProperties GetProperties(int camIndex);

        [OperationContract(IsOneWay = false)]
        bool GetIsInitialized(int camIndex);

        [OperationContract(IsOneWay = false)]
        FanMode GetFanMode(int camIndex);

        [OperationContract(IsOneWay = false)]
        Switch GetCoolerMode(int camIndex);

        [OperationContract(IsOneWay = false)]
        DeviceCapabilities GetCapabilities(int camIndex);

        [OperationContract(IsOneWay = false)]
        bool GetIsAcquiring(int camIndex);

        // TODO: Deprecate & Remove
        //[OperationContract(IsOneWay = false)]
        //bool GetIsAsyncAcquisition(int camIndex);

        [OperationContract(IsOneWay = false)]
        (ShutterMode Internal,
           ShutterMode? External,
           TtlShutterSignal Type,
           int OpenTime,
           int CloseTime) GetShutter(int camIndex);

        [OperationContract(IsOneWay = false)]
        bool GetIsTemperatureMonitored(int camIndex);

        [OperationContract(IsOneWay = false)]
        (Version EPROM, Version COFFile, Version Driver, Version Dll) GetSoftware(int camIndex);

        [OperationContract(IsOneWay = false)]
        (Version PCB, Version Decode, Version CameraFirmware) GetHardware(int camIndex);

        //[OperationContract(IsOneWay = false)]
        //(byte[] Data, int Width, int Height, TypeCode TypeCode) PullNewImage(int camIndex);


        [OperationContract(IsOneWay = false)]
        CameraStatus CallGetStatus(int camIndex);
        [OperationContract(IsOneWay = false)]
        (TemperatureStatus Status, float Temperature) CallGetCurrentTemperature(int camIndex);
        [OperationContract(IsOneWay = false)]
        void CallFanControl(int camIndex, FanMode mode);
        [OperationContract(IsOneWay = false)]
        void CallCoolerControl(int camIndex, Switch mode);
        [OperationContract(IsOneWay = false)]
        void CallSetTemperature(int camIndex, int temperature);
        [OperationContract(IsOneWay = false)]
        void CallShutterControl(
            int camIndex,
            int clTime,
            int opTime,
            ShutterMode inter,
            ShutterMode exter = ShutterMode.FullyAuto,
            TtlShutterSignal type = TtlShutterSignal.Low);

        [OperationContract(IsOneWay = false)]
        void CallTemperatureMonitor(int camIndex, Switch mode, int timeout);

        // TODO: Update this
        //[OperationContract(IsOneWay = false)]
        //void CallStartAcquisition(int camIndex);

        [OperationContract(IsOneWay = false)]
        void CallAbortAcquisition(int camIndex);



        [OperationContract(IsOneWay = false)]
        (int Index, float Speed)[] GetAvailableHSSpeeds(
            string settingsID,
            int ADConverterIndex,
            int amplifier);

        [OperationContract(IsOneWay = false)]
        (int Index, string Name)[] GetAvailablePreAmpGain(
            string settingsID,
            int ADConverterIndex,
            int amplifier,
            int HSSpeed);

        [OperationContract(IsOneWay = false)]
        (bool IsSupported, float Speed) CallIsHSSpeedSupported(
            string settingsID, 
            int ADConverter,
            int amplifier,
            int speedIndex);

        [OperationContract(IsOneWay = false)]
        ((string Option, bool Success, uint ReturnCode)[] Result, 
         (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) Timing)
         CallApplySettings(string settingsID, byte[] data);


        [OperationContract(IsOneWay = false)]
        bool IsTaskFinished(string taskID);

        [OperationContract(IsOneWay = false)]
        string CreateAcquisitionTask(int camIndex, int delay);

        [OperationContract(IsOneWay = false)]
        void RemoveTask(string taskID);

        [OperationContract(IsOneWay = false)]
        void RequestCancellation(string taskID);

        #region Async methods

        [OperationContract(IsOneWay = true)]
        void CancelAsync(RemoteCancellationToken token);

        // TODO: Attempting to build Begin/End asynchronous pattern
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginCreateCameraAsync(int camIndex, AsyncCallback callback, object state);
        bool EndCreateCameraAsync(IAsyncResult result);
        #endregion
    }
}
