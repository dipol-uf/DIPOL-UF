#nullable enable
namespace DIPOL_UF.Services.Contract
{
    internal interface ICycleTimerManager
    {
        void StartMeasuring();
        void StopMeasuring();
        void AdjustTiming();
    }
}
