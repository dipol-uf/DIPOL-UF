#nullable enable
using System;

namespace DIPOL_UF.UiComponents.Contract
{
    internal interface IJobTimerSource
    {
        IObservable<TimeSpan?> JobRemainingTime();
    }
}