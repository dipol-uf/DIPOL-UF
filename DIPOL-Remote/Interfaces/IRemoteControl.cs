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
using System.ServiceModel;

using DIPOL_Remote.Faults;
using DIPOL_Remote.Enums;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Events;

namespace DIPOL_Remote.Interfaces
{
    /// <summary>
    /// Interface used to communicate with DIPOL service.
    /// </summary>
    [ServiceContract(SessionMode = SessionMode.Required,
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
    [ServiceKnownType(typeof(SDKFeatures))]
    [ServiceKnownType(typeof(EMGain))]
    [ServiceKnownType(typeof(CameraStatus))]
    [ServiceKnownType(typeof(ShutterMode))]
    [ServiceKnownType(typeof(TTLShutterSignal))]
    [ServiceKnownType(typeof(TemperatureStatus))]
    [ServiceKnownType(typeof(AcquisitionEventType))]
    [ServiceKnownType(typeof(AcquisitionStatusEventArgs))]
    [ServiceKnownType(typeof(TemperatureStatusEventArgs))]
    [ServiceKnownType(typeof(NewImageReceivedEventArgs))]
    [ServiceKnownType(typeof(ImageDisplayLib.Image))]
    [ServiceKnownType(typeof(TypeCode))]
    internal interface IRemoteControl
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
        [FaultContract(typeof(ServiceException))]
        void Connect();
        /// <summary>
        /// Exit point of the connection. Frees resources
        /// </summary>
        [OperationContract(IsTerminating = true, IsOneWay = false)]
        [FaultContract(typeof(ServiceException))]
        void Disconnect();

        /// <summary>
        /// Returns number of available cameras
        /// </summary>
        /// <returns></returns>
        [OperationContract(IsOneWay = false)]
        [FaultContract(typeof(AndorSDKServiceException))]
        [FaultContract(typeof(ServiceException))]
        int GetNumberOfCameras();

        [OperationContract(IsOneWay = false)]
        [FaultContract(typeof(AndorSDKServiceException))]
        [FaultContract(typeof(ServiceException))]
        void CreateCamera(int camIndex = 0);

        [OperationContract(IsOneWay = false)]
        [FaultContract(typeof(ServiceException))]
        void RemoveCamera(int camIndex);

        [OperationContract(IsOneWay = false)]
        int[] GetCamerasInUse();

        [OperationContract(IsOneWay = false)]
        [FaultContract(typeof(ServiceException))]
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
        [OperationContract(IsOneWay = false)]
        bool GetIsAsyncAcquisition(int camIndex);
        [OperationContract(IsOneWay = false)]
        (ShutterMode Internal,
           ShutterMode? External,
           TTLShutterSignal Type,
           int OpenTime,
           int CloseTime) GetShutter(int camIndex);
        [OperationContract(IsOneWay = false)]
        (Version EPROM, Version COFFile, Version Driver, Version Dll) GetSoftware(int camIndex);
        [OperationContract(IsOneWay = false)]
        (Version PCB, Version Decode, Version CameraFirmware) GetHardware(int camIndex);

        [OperationContract(IsOneWay = false)]
        (byte[] Data, int Width, int Height, TypeCode TypeCode) PullNewImage(int camIndex);


        [OperationContract(IsOneWay = false)]
        CameraStatus CallGetStatus(int camIndex);
        [OperationContract(IsOneWay = false)]
        (TemperatureStatus Status, float Temperature) CallGetCurrentTemperature(int camIndex);
        [OperationContract(IsOneWay = false)]
        void CallSetActive(int camIndex);
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
            TTLShutterSignal type = TTLShutterSignal.Low);
        [OperationContract(IsOneWay = false)]
        void CallTemperatureMonitor(int camIndex, Switch mode, int timeout);
        [OperationContract(IsOneWay = false)]
        void CallStartAcquisition(int camIndex);
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
    }
}
