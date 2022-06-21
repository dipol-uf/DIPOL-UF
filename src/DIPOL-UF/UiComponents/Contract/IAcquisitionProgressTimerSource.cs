#nullable enable
using System;

namespace DIPOL_UF.UiComponents.Contract
{
    public interface IAcquisitionProgressTimerSource
    {
        IObservable<TimeSpan?> AcquisitionRemainingTime(IObservable<bool> isAcquiring);
    }
}