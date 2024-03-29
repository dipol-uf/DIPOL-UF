﻿//    This file is part of Dipol-3 Camera Manager.

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


using System.ServiceModel;
using ANDOR_CS.Events;
using DIPOL_Remote.Enums;

namespace DIPOL_Remote.Callback
{
    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        IncludeExceptionDetailInFaults = true)]
    internal class RemoteCallbackHandler : IRemoteCallback
    {

        public void NotifyRemoteAcquisitionEventHappened(int camIndex,
            AcquisitionEventType type, AcquisitionStatusEventArgs args)
       => RemoteCamera.NotifyRemoteAcquisitionEventHappened(camIndex, type, args);

        public void NotifyRemotePropertyChanged(int camIndex, string property)
            => RemoteCamera.NotifyRemotePropertyChanged(camIndex, property);

        public void NotifyRemoteTemperatureStatusChecked(int camIndex, TemperatureStatusEventArgs args)
            => RemoteCamera.NotifyRemoteTemperatureStatusChecked(camIndex, args);

        public void NotifyRemoteNewImageReceivedEventHappened(int camIndex, NewImageReceivedEventArgs e)
            => RemoteCamera.NotifyRemoteNewImageReceivedEventHappened(camIndex, e);

        public void NotifyRemoteImageSavedEventHappened(int camIndex, ImageSavedEventArgs e)
            => RemoteCamera.NotifyRemoteImageSavedEventHappened(camIndex, e);
    }
}
