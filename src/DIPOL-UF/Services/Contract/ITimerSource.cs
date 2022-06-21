#nullable enable
namespace DIPOL_UF.Services.Contract
{
    internal interface ITimerSource
    {
        ITimer? GetIfRunning();
    }
    
    internal interface ICycleTimerSource : ITimerSource {}

    internal interface IAcquisitionTimerSource : ITimerSource
    {
    }

}
