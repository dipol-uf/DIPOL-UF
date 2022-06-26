using DIPOL_Remote;
using DIPOL_UF.Services.Contract;

namespace DIPOL_UF.Services.Implementation
{
    internal sealed class RemoteDeviceFactoryConstructor : IRemoteDeviceFactoryConstructor
    {
        public IRemoteDeviceFactory Construct(IControlClient client) => new RemoteCamera.RemoteCameraFactory(client);
    }
}
