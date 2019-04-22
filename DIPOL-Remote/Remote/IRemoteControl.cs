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
using ANDOR_CS.AcquisitionMetadata;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DIPOL_Remote.Callback;
using DIPOL_Remote.Enums;
using DIPOL_Remote.Faults;
using FITS_CS;

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
    [ServiceKnownType(typeof(TypeCode))]
    [ServiceKnownType(typeof(ImageFormat))]
    [ServiceKnownType(typeof(RemoteCancellationToken))]
    [ServiceKnownType(typeof(FitsKey))]
    public interface IRemoteControl
    {
        /// <summary>
        /// Unique ID of the session 
        /// </summary>
        string SessionID
        {
            [OperationContract]
            get;
        }

        /// <summary>
        /// Entry point of the connection
        /// </summary>
        [OperationContract(IsInitiating = true)]
        [FaultContract(typeof(ServiceFault))]
        void Connect();
        /// <summary>
        /// Exit point of the connection. Frees resources
        /// </summary>
        [OperationContract(IsTerminating = true)]
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

        [OperationContract]
        [FaultContract(typeof(AndorSdkFault))]
        [FaultContract(typeof(ServiceFault))]
        void CreateCamera(int camIndex = 0);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        void RemoveCamera(int camIndex);

        [OperationContract(IsOneWay = false)]
        int[] GetCamerasInUse();

        [OperationContract(IsOneWay = false)]
        [FaultContract(typeof(ServiceFault))]
        [FaultContract(typeof(AndorSdkFault))]
        string CreateSettings(int camIndex);

        [OperationContract]
        void RemoveSettings(string settingsID);

        [OperationContract(IsOneWay = false)]
        string CallMakeCopy(string settingsId);


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

        [OperationContract(IsOneWay = false)]
        CameraStatus CallGetStatus(int camIndex);

        [OperationContract(IsOneWay = false)]
        (TemperatureStatus Status, float Temperature) CallGetCurrentTemperature(int camIndex);

        [OperationContract]
        void CallFanControl(int camIndex, FanMode mode);

        [OperationContract]
        void CallCoolerControl(int camIndex, Switch mode);

        [OperationContract]
        void CallSetTemperature(int camIndex, int temperature);

        [OperationContract]
        void CallShutterControl(
            int camIndex,
            ShutterMode inter,
            ShutterMode exter,
            int clTime,
            int opTime,
            TtlShutterSignal type = TtlShutterSignal.Low);

        [OperationContract]
        void CallTemperatureMonitor(int camIndex, Switch mode, int timeout);

        
        [OperationContract(IsOneWay = false)]
        (int Index, float Speed)[] GetAvailableHsSpeeds(
            string settingsID,
            int adConverterIndex,
            int amplifier);

        [OperationContract(IsOneWay = false)]
        (int Index, string Name)[] GetAvailablePreAmpGain(
            string settingsID,
            int adConverterIndex,
            int amplifier,
            int hsSpeed);

        [OperationContract(IsOneWay = false)]
        (bool IsSupported, float Speed) CallIsHsSpeedSupported(
            string settingsID, 
            int adConverter,
            int amplifier,
            int speedIndex);

        [OperationContract(IsOneWay = false)]
        (int Low, int High) CallGetEmGainRange(string settingsId);

        [OperationContract]
        void CallApplySetting(int camIndex, string settingsId, byte[] payload);

        [OperationContract]
        (float Exposure, float Accumulate, float Kinetic)
            CallGetTimings(int camIndex);



        [OperationContract(IsOneWay = false)]
        (byte[] Payload, int Width, int Height) 
            CallPullPreviewImage(int camIndex, int imageIndex, ImageFormat format);

        [OperationContract]
        int CallGetTotalNumberOfAcquiredImages(int camIndex);

        [OperationContract]
        void CallSetAutosave(int camIndex, Switch mode, ImageFormat format);

        [OperationContract]
        [Obsolete]
        void CallSaveNextAcquisitionAs(
            int camIndex,
            string folderPath,
            string imagePattern,
            ImageFormat format,
            FitsKey[] extraKeys);

        [OperationContract]
        void StartImageSavingSequence(
            int camIndex,
            string folderPath, string imagePattern,
            string filter, FitsKey[] extraKeys = null);

        #region Async methods

        [OperationContract(IsOneWay = true)]
        void CancelAsync(RemoteCancellationToken token);

        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginCreateCameraAsync(int camIndex, AsyncCallback callback, object state);
        void EndCreateCameraAsync(IAsyncResult result);

        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginStartAcquisitionAsync(int camIndex, Request metadata, RemoteCancellationToken token, AsyncCallback callback, object state);
        void EndStartAcquisitionAsync(IAsyncResult result);

        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginPullAllImagesAsync(int camIndex, ImageFormat format, RemoteCancellationToken token,
            AsyncCallback callback, object state);
        (byte[] Payload, int Width, int Height)[] EndPullAllImagesAsync(IAsyncResult result);


        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginFinishImageSavingSequence(int camIndex, AsyncCallback callback, object state);
        void EndFinishImageSavingSequence(IAsyncResult result);

        #endregion
    }
}
