#if DEBUG
#nullable enable

using System.Threading.Tasks;
using ANDOR_CS;

namespace DIPOL_UF
{
    internal class DebugLocalDeviceFactory : IDeviceFactory
    {

        private readonly IDeviceFactory _localFactory;
        private readonly IDebugDeviceFactory _debugFactory;

        public DebugLocalDeviceFactory(IDeviceFactory localFactory, IDebugDeviceFactory debugFactory) =>
            (_localFactory, _debugFactory) = (localFactory, debugFactory);

        public int GetNumberOfCameras() => _localFactory.GetNumberOfCameras() is var nCams and > 0
            ? nCams
            : _debugFactory.GetNumberOfCameras();

        public IDevice Create(int index = 0) =>
            _localFactory.GetNumberOfCameras() > 0
                ? _localFactory.Create(index)
                : _debugFactory.Create(index);

        public Task<IDevice> CreateAsync(int index = 0) =>
            _localFactory.GetNumberOfCameras() > 0
                ? _localFactory.CreateAsync(index)
                : _debugFactory.CreateAsync(index);
    }
}
#endif