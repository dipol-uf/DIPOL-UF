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
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using DIPOL_Remote.Interfaces;

namespace DIPOL_Remote.Classes
{
    public class DipolClient : DuplexClientBase<IRemoteControl>, IRemoteControl
    {
        internal static ConcurrentDictionary<(string sessionID, int camIndex), (ManualResetEvent Event, bool Success)>
            CameraCreatedEvents { get; } =
            new ConcurrentDictionary<(string sessionID, int camIndex), (ManualResetEvent, bool)>();

        [Obsolete]
        public IRemoteControl Remote => Channel;

        public string HostAddress => Endpoint.Address.ToString();

        public string SessionID
            => Channel.SessionID;

        public DipolClient(Uri hostUri)
        : base(new InstanceContext(new RemoteCallbackHandler()),
                new NetTcpBinding(SecurityMode.None), 
                new EndpointAddress(hostUri))
        {
            //HostAddress = hostUri.ToString();
            //var bnd = new NetTcpBinding(SecurityMode.None)
            //{
            //    MaxReceivedMessageSize = 512 * 512 * 8 * 2
            //};
            //// IMPORTANT! Limits the size of SOAP message. For larger images requires another implementation
            //_remote = new DuplexChannelFactory<IRemoteControl>(
            //    _context,
            //    bnd,
            //    new EndpointAddress(hostUri)).CreateChannel();

        }

        public DipolClient(Uri hostUri, 
            TimeSpan openTimeout, 
            TimeSpan sendTimeout, 
            TimeSpan operationTimeout, 
            TimeSpan closeTimeout) :  this(hostUri)
        {
            //HostAddress = hostUri.ToString();
            //var bnd = new NetTcpBinding(SecurityMode.None)
            //{
            //    MaxReceivedMessageSize = 512 * 512 * 8 * 2,
            //    OpenTimeout = openTimeout,
            //    SendTimeout = sendTimeout,
            //    CloseTimeout = closeTimeout
            //};
            //// IMPORTANT! Limits the size of SOAP message. For larger images requires another implementation


            //_remote = new DuplexChannelFactory<IRemoteControl>(
            //    _context,
            //    bnd,
            //    new EndpointAddress(hostUri)).CreateChannel();

            //// ReSharper disable once SuspiciousTypeConversion.Global
            //((IDuplexContextChannel) _remote).OperationTimeout = operationTimeout;
        }


        public void Connect()
        {
            Open();
            Channel.Connect();
        }

        public void Disconnect()
        {
            Channel.Disconnect();
            Close();
        }

        public void Dispose()
        {
            Close();
        }


        #region Remote interface implementations

        public int GetNumberOfCameras()
            => Channel.GetNumberOfCameras();

        public void CreateCamera(int camIndex = 0)
			=> Channel.CreateCamera(camIndex);

        public void RequestCreateCamera(int camIndex)
			=> Channel.RequestCreateCamera(camIndex);

        public void RemoveCamera(int camIndex)
			=> Channel.RemoveCamera(camIndex);

        public int[] GetCamerasInUse()
			=> Channel.GetCamerasInUse();

        public string CreateSettings(int camIndex)
			=> Channel.CreateSettings(camIndex);

        public void RemoveSettings(string settingsID)
			=> Channel.RemoveSettings(settingsID);

        public string GetCameraModel(int camIndex)
			=> Channel.GetCameraModel(camIndex);

        public bool GetIsActive(int camIndex)
			=> Channel.GetIsActive(camIndex);

        public string GetSerialNumber(int camIndex)
			=> Channel.GetSerialNumber(camIndex);

        public CameraProperties GetProperties(int camIndex)
			=> Channel.GetProperties(camIndex);

        public bool GetIsInitialized(int camIndex)
			=> Channel.GetIsInitialized(camIndex);

        public FanMode GetFanMode(int camIndex)
			=> Channel.GetFanMode(camIndex);

        public Switch GetCoolerMode(int camIndex)
			=> Channel.GetCoolerMode(camIndex);

        public DeviceCapabilities GetCapabilities(int camIndex)
			=> Channel.GetCapabilities(camIndex);

        public bool GetIsAcquiring(int camIndex)
			=> Channel.GetIsAcquiring(camIndex);

        public (ShutterMode Internal, ShutterMode? External, TtlShutterSignal Type, int OpenTime, int CloseTime)
            GetShutter(int camIndex)
            => Channel.GetShutter(camIndex);

        public bool GetIsTemperatureMonitored(int camIndex)
			=> Channel.GetIsTemperatureMonitored(camIndex);

        public (Version EPROM, Version COFFile, Version Driver, Version Dll) GetSoftware(int camIndex)
            => Channel.GetSoftware(camIndex);

        public (Version PCB, Version Decode, Version CameraFirmware) GetHardware(int camIndex)
            => Channel.GetHardware(camIndex);

        public CameraStatus CallGetStatus(int camIndex)
			=> Channel.CallGetStatus(camIndex);

        public (TemperatureStatus Status, float Temperature) CallGetCurrentTemperature(int camIndex)
            => Channel.CallGetCurrentTemperature(camIndex);

        public void CallFanControl(int camIndex, FanMode mode)
			=> Channel.CallFanControl(camIndex, mode);

        public void CallCoolerControl(int camIndex, Switch mode)
			=> Channel.CallCoolerControl(camIndex, mode);

        public void CallSetTemperature(int camIndex, int temperature)
			=> Channel.CallSetTemperature(camIndex, temperature);

        public void CallShutterControl(int camIndex, int clTime, int opTime, ShutterMode inter,
            ShutterMode exter = ShutterMode.FullyAuto, TtlShutterSignal type = TtlShutterSignal.Low)
            => Channel.CallShutterControl(camIndex, clTime, opTime, inter, exter, type);

        public void CallTemperatureMonitor(int camIndex, Switch mode, int timeout)
			=> Channel.CallTemperatureMonitor(camIndex, mode, timeout);

        public void CallAbortAcquisition(int camIndex)
			=> Channel.CallAbortAcquisition(camIndex);

        public (int Index, float Speed)[] GetAvailableHSSpeeds(string settingsID, int ADConverterIndex, int amplifier)
            => Channel.GetAvailableHSSpeeds(settingsID, ADConverterIndex, amplifier);

        public (int Index, string Name)[] GetAvailablePreAmpGain(string settingsID, int ADConverterIndex, int amplifier, int HSSpeed)
            => Channel.GetAvailablePreAmpGain(settingsID, ADConverterIndex, amplifier, HSSpeed);

        public (bool IsSupported, float Speed) CallIsHSSpeedSupported(string settingsID, int ADConverter, int amplifier,
            int speedIndex)
            => Channel.CallIsHSSpeedSupported(settingsID, ADConverter, amplifier, speedIndex);

        // TODO: Fix this
        public ((string Option, bool Success, uint ReturnCode)[] Result, (float ExposureTime, float AccumulationCycleTime, float
            KineticCycleTime, int BufferSize) Timing) CallApplySettings(string settingsID, byte[] data)
            => CallApplySettings(settingsID, data);

        public bool IsTaskFinished(string taskID)
			=> Channel.IsTaskFinished(taskID);

        public string CreateAcquisitionTask(int camIndex, int delay)
			=> Channel.CreateAcquisitionTask(camIndex,  delay);

        public void RemoveTask(string taskID)
			=> Channel.RemoveTask(taskID);

        public void RequestCancellation(string taskID)
			=> Channel.RequestCancellation(taskID);


        public int[] ActiveRemoteCameras()
            => Channel.GetCamerasInUse();

        public void CreateRemoteCamera(int camIndex = 0)
        {
            if (Channel.GetCamerasInUse().Contains(camIndex))
                throw new ArgumentException($"Camera with index {camIndex} is already in use.");
            Channel.CreateCamera(camIndex);
        }

        public void RequestCreateRemoteCamera(int camIndex = 0)
        {
            if (Channel.GetCamerasInUse().Contains(camIndex))
                throw new ArgumentException($"Camera with index {camIndex} is already in use.");
            Channel.RequestCreateCamera(camIndex);
        }

        #endregion
        
        #region Explicit async unused implementations

        IAsyncResult IRemoteControl.BeginCreateCameraAsync(int camIndex, AsyncCallback callback, object state)
			=> Channel.BeginCreateCameraAsync(camIndex, callback, state);
        bool IRemoteControl.EndCreateCameraAsync(IAsyncResult result)
			=> Channel.EndCreateCameraAsync(result);

#endregion
        
        #region TAP async implementations

        #endregion
    }
}
