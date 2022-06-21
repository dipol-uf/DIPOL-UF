#nullable enable
namespace DIPOL_UF.Services.Contract
{
    internal interface ITimerManager<in T>
    {
        void StartMeasuring(T cycleTimingInfo);
        void StopMeasuring();
        void PauseMeasuring();
        void AdjustTiming(T cycleTimingInfo);
    }
}
