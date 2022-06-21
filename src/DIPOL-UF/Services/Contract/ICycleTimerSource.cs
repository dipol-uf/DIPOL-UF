#nullable enable
namespace DIPOL_UF.Services.Contract
{
    internal interface ICycleTimerSource
    {
        ICycleTimer? GetIfRunning();
    }
}
