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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
#if X86
using SDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using SDK = ATMCD64CS.AndorSDK;
#endif

namespace ANDOR_CS
{
    public class LocalCameraFactory : IDeviceFactory
    {
        public int GetNumberOfCameras()
        {
            // Variable is passed to SDK function

            var result =
                Classes.AndorSdkInitialization.CallWithoutHandle(
                    Classes.AndorSdkInitialization.SdkInstance.GetAvailableCameras, out int cameraCount);
            if (Exceptions.AndorSdkException.FailIfError(result,
                nameof(Classes.AndorSdkInitialization.SdkInstance.GetAvailableCameras), out var except))
                throw except;

            return cameraCount;
        }

        public IDevice Create(int index = 0, IDeviceCreationParams @params = null)
            => new Camera(index);

        public Task<IDevice> CreateAsync(int index = 0, IDeviceCreationParams @params = null)
            => Task.Run(() => Create(index, @params));
    }
}
