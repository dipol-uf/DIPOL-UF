#if DEBUG
#nullable enable
using System;
using System.Threading.Tasks;
using ANDOR_CS.Exceptions;


namespace ANDOR_CS.Classes
{
    public sealed partial class DebugCamera
    {
        public class DebugCameraFactory : IDebugDeviceFactory
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public int GetNumberOfCameras() => 3;

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

                return new DebugCamera(index);

            }

            public Task<IDevice> CreateAsync(int index = 0)
                => Task.Run(() => Create(index));
        }
    }
}
#endif
