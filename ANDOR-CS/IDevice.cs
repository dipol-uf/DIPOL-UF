//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
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
#nullable enable
#pragma warning disable 1591
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS.AcquisitionMetadata;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DipolImage;
using FITS_CS;

namespace ANDOR_CS
{
    public interface IDevice : IDisposable, INotifyPropertyChanged
    {

        event TemperatureStatusEventHandler TemperatureStatusChecked;

        int CameraIndex { get; }
        string CameraModel { get; }
        string SerialNumber { get; }
        bool IsDisposed { get; }
        bool IsAcquiring { get; }
        (Version EPROM, Version COFFile, Version Driver, Version Dll) Software { get; }
        (Version PCB, Version Decode, Version CameraFirmware) Hardware { get; }

        FanMode FanMode { get; }

        DeviceCapabilities Capabilities { get; }
        CameraProperties Properties { get; }

        CameraStatus GetStatus();
        (TemperatureStatus Status, float Temperature) GetCurrentTemperature();
        void FanControl(FanMode mode);
        void CoolerControl(Switch mode);
        void SetTemperature(int temperature);
        void ShutterControl(ShutterMode inter, ShutterMode extrn);
        void TemperatureMonitor(Switch mode, int timeout);

        Image PullPreviewImage(int index, ImageFormat format);

        Task<Image[]> PullAllImagesAsync<T>(CancellationToken token) where T : unmanaged;
        Task<Image[]> PullAllImagesAsync(ImageFormat format, CancellationToken token);

        void StartImageSavingSequence(
            string folderPath, string imagePattern,
            string? filter, FrameType type = FrameType.Light,
            FitsKey[]? extraKeys = null);

        Task FinishImageSavingSequenceAsync();

        Image? PullPreviewImage<T>(int index) where T : unmanaged;

        int GetTotalNumberOfAcquiredImages();

        Task StartAcquisitionAsync(Request? metadata = default, CancellationToken token = default);

        void CheckIsDisposed();

        void ApplySettings(IAcquisitionSettings settings);

        IAcquisitionSettings GetAcquisitionSettingsTemplate();
    }
}