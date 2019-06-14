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
using System.Threading.Tasks;
using ANDOR_CS.Exceptions;


namespace ANDOR_CS.Classes
{
    public sealed partial class Camera
    {
        public class LocalCameraFactory : IDeviceFactory
        {
            public int GetNumberOfCameras()
            {
                // Variable is passed to SDK function

                var result =
                    AndorSdkInitialization.CallWithoutHandle(
                        AndorSdkInitialization.SdkInstance.GetAvailableCameras, out int cameraCount);
                if (AndorSdkException.FailIfError(result,
                    nameof(AndorSdkInitialization.SdkInstance.GetAvailableCameras),
                    out var except))
                    throw except;

                return cameraCount;
            }

            public IDevice Create(int index = 0)
            {
                var n = GetNumberOfCameras();
                if (n == 0)
                    throw new AndorSdkException("No ANDOR-compatible cameras found.", null);

                // If cameraIndex is less than 0, it is out of range
                if (index < 0)
                    throw new ArgumentException(
                        $"Camera index is out of range; Cannot be less than 0 (provided {index}).");
                // If cameraIndex equals to or exceeds the number of available cameras, it is also out of range
                if (index > n)
                    throw new ArgumentException(
                        $"Camera index is out of range; Cannot be greater than {GetNumberOfCameras() - 1} (provided {index}).");

                return new Camera(index);

            }

            public Task<IDevice> CreateAsync(int index = 0)
                => Task.Run(() => Create(index));
        }
    }
}
