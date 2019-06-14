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
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS.AcquisitionMetadata;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using DIPOL_Remote.Callback;
using DIPOL_Remote.Faults;
using DIPOL_Remote.Remote;
using FITS_CS;

namespace DIPOL_Remote
{
    public class DipolClient : DuplexClientBase<IRemoteControl>, IRemoteControl
    {
        private static readonly int MaxMessageSize = 512 * 512 * 8 * 4;

        public string HostAddress => Endpoint.Address.ToString();

        public string SessionID
            => Channel.SessionID;

        private DipolClient(InstanceContext context, Binding binding, EndpointAddress endpoint)
            : base(context, binding, endpoint)
        {

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


        public static DipolClient Create(Uri hostUri)
        {
            var context = new InstanceContext(new RemoteCallbackHandler());
            var binding = new NetTcpBinding(SecurityMode.None)
            {
                MaxBufferSize = MaxMessageSize,
                MaxReceivedMessageSize = MaxMessageSize
            };
            var endpoint = new EndpointAddress(hostUri);

            return new DipolClient(context, binding, endpoint);
        }

        public static DipolClient Create(Uri hostUri,
            TimeSpan openTimeout,
            TimeSpan sendTimeout,
            TimeSpan closeTimeout)
        {
            var context = new InstanceContext(new RemoteCallbackHandler());
            var binding = new NetTcpBinding(SecurityMode.None)
            {
                MaxBufferSize = MaxMessageSize,
                MaxReceivedMessageSize = MaxMessageSize,
                OpenTimeout = openTimeout,
                SendTimeout = sendTimeout,
                CloseTimeout = closeTimeout
            };
            var endpoint = new EndpointAddress(hostUri);

            return new DipolClient(context, binding, endpoint);
        }

        #region Remote interface implementations

        public int GetNumberOfCameras()
            => Channel.GetNumberOfCameras();

        public void CreateCamera(int camIndex = 0)
			=> Channel.CreateCamera(camIndex);

        public void RemoveCamera(int camIndex)
			=> Channel.RemoveCamera(camIndex);

        public int[] GetCamerasInUse()
			=> Channel.GetCamerasInUse();

        public string CreateSettings(int camIndex)
			=> Channel.CreateSettings(camIndex);

        public void RemoveSettings(string settingsId)
			=> Channel.RemoveSettings(settingsId);

        public string CallMakeCopy(string settingsId)
            => Channel.CallMakeCopy(settingsId);

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

        public void CallShutterControl(int camIndex,
            ShutterMode inter, ShutterMode @extern, 
            int clTime, int opTime,
            TtlShutterSignal type = TtlShutterSignal.Low)
            => Channel.CallShutterControl(camIndex, inter, @extern, clTime, opTime, type);

        public void CallTemperatureMonitor(int camIndex, Switch mode, int timeout)
			=> Channel.CallTemperatureMonitor(camIndex, mode, timeout);



        public (int Index, float Speed)[] GetAvailableHsSpeeds(string settingsID, int adConverterIndex, int amplifier)
            => Channel.GetAvailableHsSpeeds(settingsID, adConverterIndex, amplifier);

        public (int Index, string Name)[] GetAvailablePreAmpGain(string settingsID, int adConverterIndex, int amplifier, int hsSpeed)
            => Channel.GetAvailablePreAmpGain(settingsID, adConverterIndex, amplifier, hsSpeed);

        public (bool IsSupported, float Speed) CallIsHsSpeedSupported(string settingsID, int adConverter, int amplifier,
            int speedIndex)
            => Channel.CallIsHsSpeedSupported(settingsID, adConverter, amplifier, speedIndex);

        public (int Low, int High) CallGetEmGainRange(string settingsId)
            => Channel.CallGetEmGainRange(settingsId);

        public void CallApplySetting(int camIndex, string settingsId, byte[] payload)
            => Channel.CallApplySetting(camIndex, settingsId, payload);

        public (float Exposure, float Accumulate, float Kinetic) CallGetTimings(int camIndex)
            => Channel.CallGetTimings(camIndex);

        public (byte[] Payload, int Width, int Height) 
            CallPullPreviewImage(int camIndex, int imageIndex, ImageFormat format)
            => Channel.CallPullPreviewImage(camIndex, imageIndex, format);

        public int CallGetTotalNumberOfAcquiredImages(int camIndex)
            => Channel.CallGetTotalNumberOfAcquiredImages(camIndex);

        public void CallSetAutosave(int camIndex, Switch mode, ImageFormat format)
            => Channel.CallSetAutosave(camIndex, mode, format);

        public void CallStartImageSavingSequence(int camIndex, string folderPath, string imagePattern, string filter,
            FitsKey[] extraKeys = null)
            => Channel.CallStartImageSavingSequence(camIndex, folderPath, imagePattern, filter, extraKeys);

        public int[] ActiveRemoteCameras()
            => Channel.GetCamerasInUse();

        public void CreateRemoteCamera(int camIndex = 0)
        {
            if (Channel.GetCamerasInUse().Contains(camIndex))
                throw new ArgumentException($"Camera with index {camIndex} is already in use.");
            Channel.CreateCamera(camIndex);
        }

        #endregion
        
        #region Explicit async unused implementations

        void IRemoteControl.CancelAsync(RemoteCancellationToken token)
            => throw new NotSupportedException(
                $"{nameof(IRemoteControl.CancelAsync)} is not supported directly. " +
                $"Use respective {nameof(RemoteCancellationToken)} to cancel an async operation.");

        IAsyncResult IRemoteControl.BeginCreateCameraAsync(int camIndex, AsyncCallback callback, object state)
			=> throw new NotSupportedException(
                $"{nameof(IRemoteControl.BeginCreateCameraAsync)} is not supported directly. " +
                $"Use {nameof(CreateCameraAsync)} instead.");
        void IRemoteControl.EndCreateCameraAsync(IAsyncResult result)
            => throw new NotSupportedException(
                $"{nameof(IRemoteControl.EndCreateCameraAsync)} is not supported directly. " +
                $"Use {nameof(CreateCameraAsync)} instead.");

        IAsyncResult IRemoteControl.BeginStartAcquisitionAsync(int camIndex, Request metadata, RemoteCancellationToken token, AsyncCallback callback,
            object state)
            => throw new NotSupportedException(
                $"{nameof(IRemoteControl.BeginStartAcquisitionAsync)} is not supported directly. " +
                $"Use {nameof(StartAcquisitionAsync)} instead.");
        void IRemoteControl.EndStartAcquisitionAsync(IAsyncResult result)
            => throw new NotSupportedException(
                $"{nameof(IRemoteControl.EndStartAcquisitionAsync)} is not supported directly. " +
                $"Use {nameof(StartAcquisitionAsync)} instead.");

        IAsyncResult IRemoteControl.BeginPullAllImagesAsync(int camIndex, ImageFormat format, RemoteCancellationToken token,
            AsyncCallback callback, object state)
            => throw new NotSupportedException(
                $"{nameof(IRemoteControl.BeginPullAllImagesAsync)} is not supported directly. " +
                $"Use {nameof(PullAllImagesAsync)} instead.");
        (byte[] Payload, int Width, int Height)[] IRemoteControl.EndPullAllImagesAsync(IAsyncResult result)
            => throw new NotSupportedException(
                $"{nameof(IRemoteControl.EndPullAllImagesAsync)} is not supported directly. " +
                $"Use {nameof(PullAllImagesAsync)} instead.");

        public IAsyncResult BeginFinishImageSavingSequence(int camIndex, AsyncCallback callback, object state)
            => throw new NotSupportedException(
                $"{nameof(IRemoteControl.BeginFinishImageSavingSequence)} is not supported directly. " +
                $"Use {nameof(FinishImageSavingSequenceAsync)} instead.");

        public void EndFinishImageSavingSequence(IAsyncResult result)
            => throw new NotSupportedException(
                $"{nameof(IRemoteControl.EndFinishImageSavingSequence)} is not supported directly. " +
                $"Use {nameof(FinishImageSavingSequenceAsync)} instead.");

        #endregion

        #region TAP async implementations

        private static void FinalizeAsyncOperation<T>(object eventArgs, TaskCompletionSource<T> source)
        {
            if (!(eventArgs is InvokeAsyncCompletedEventArgs e))
                throw new ArgumentException("Invalid finalization state", nameof(eventArgs));
            switch (e.Error)
            {
                // e.Cancelled appears to be always false
                case null when e.Cancelled:
                    source.SetCanceled();
                    break;
                case null when e.Results.Length == 1:
                    source.SetResult(e.Results[0] is T result ? result : default);
                    break;
                case null:
                    source.SetException(new InvalidOperationException("Inconsistent async operation result"));
                    break;
                case FaultException<TaskCancelledRemotelyFault> _:
                    source.SetCanceled();
                    break;
                default:
                    source.SetException(e.Error);
                    break;
            }
        }

        private async Task AsyncHelper<TParam>(
            Func<TParam, AsyncCallback, object, IAsyncResult> beginInvoke,
            Action<IAsyncResult> endInvoke,
            TParam value)
        {
            var taskSource = new TaskCompletionSource<bool>();

            InvokeAsync(
                (@params, callback, state) => beginInvoke((TParam) @params[0], callback, state),
                new object[] {value},
                result =>
                {
                    endInvoke(result);
                    return new object[] {true};
                }, state => FinalizeAsyncOperation(state, taskSource), null);

            await taskSource.Task;
        }

        // ReSharper disable once UnusedMember.Local
        private async Task<TResult> AsyncHelper<TParam, TResult>(
            Func<TParam, RemoteCancellationToken, AsyncCallback, object, IAsyncResult> beginInvoke,
            Func<IAsyncResult, TResult> endInvoke,
            TParam value,
            CancellationToken token)
        {
            var taskSource = new TaskCompletionSource<TResult>();

            var remoteToken = RemoteCancellationToken.CreateFromToken(token);

            InvokeAsync(
                (@params, callback, state) => 
                    beginInvoke((TParam)@params[0], (RemoteCancellationToken)@params[1], callback, state),
                new object[] { value, remoteToken },
                result => new object[] { endInvoke(result)}, 
                state => FinalizeAsyncOperation(state, taskSource),
                null);

            token.Register(() => Channel.CancelAsync(remoteToken));

            return await taskSource.Task;
        }

        private async Task<TResult> AsyncHelper<TParam1, TParam2, TResult>(
            Func<TParam1, TParam2, RemoteCancellationToken, AsyncCallback, object, IAsyncResult> beginInvoke,
            Func<IAsyncResult, TResult> endInvoke,
            TParam1 param1,
            TParam2 param2,
            CancellationToken token)
        {
            var taskSource = new TaskCompletionSource<TResult>();

            var remoteToken = RemoteCancellationToken.CreateFromToken(token);

            InvokeAsync(
                (@params, callback, state) =>
                    beginInvoke(
                        (TParam1)@params[0], (TParam2)@params[1], 
                        (RemoteCancellationToken)@params[2], callback, state),
                new object[] { param1, param2, remoteToken },
                result => new object[] { endInvoke(result) },
                state => FinalizeAsyncOperation(state, taskSource),
                null);

            token.Register(() => Channel.CancelAsync(remoteToken));

            return await taskSource.Task;
        }

        public Task FinishImageSavingSequenceAsync(int camIndex)
            => AsyncHelper(Channel.BeginFinishImageSavingSequence,
                Channel.EndFinishImageSavingSequence,
                camIndex);

        public Task CreateCameraAsync(int camIndex)
            => AsyncHelper(
                Channel.BeginCreateCameraAsync,
                x => Channel.EndCreateCameraAsync(x),
                camIndex);

        public Task StartAcquisitionAsync(int camIndex, Request metadata, CancellationToken token)
            => AsyncHelper(
                Channel.BeginStartAcquisitionAsync,
                x =>
                {
                    Channel.EndStartAcquisitionAsync(x);
                    return true;
                },
                camIndex,
                metadata,
                token);

        public Task<DipolImage.Image[]> PullAllImagesAsync(int camIndex, ImageFormat format, CancellationToken token)
            => AsyncHelper(
                    Channel.BeginPullAllImagesAsync,
                    Channel.EndPullAllImagesAsync,
                    camIndex,
                    format,
                    token)
                .ContinueWith(x => x.Result.Select(y => new DipolImage.Image(y.Payload, y.Width, y.Height,
                                        format == ImageFormat.SignedInt32 ? TypeCode.Int32 : TypeCode.UInt16))
                                    .ToArray(), token);
        

        #endregion
    }
}
