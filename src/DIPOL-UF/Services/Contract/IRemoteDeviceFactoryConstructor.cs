using DIPOL_Remote;

namespace DIPOL_UF.Services.Contract
{
    internal interface IRemoteDeviceFactoryConstructor
    {
        IRemoteDeviceFactory Construct(IControlClient client);
    }
}
