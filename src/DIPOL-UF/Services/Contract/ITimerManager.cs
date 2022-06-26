#nullable enable
using System;

namespace DIPOL_UF.Services.Contract
{
    internal interface ITimerManager<in T>
    {
        void StartMeasuring(T timingInfo);
        void StopMeasuring();
        void PauseMeasuring();
        void AdjustTiming(T timingInfo);
    }

    internal interface ICycleTimerManager : ITimerManager<CycleTimingInfo>
    {

    }

    internal interface IAcquisitionTimerManager : ITimerManager<TimeSpan>
    {
        
    }

}
