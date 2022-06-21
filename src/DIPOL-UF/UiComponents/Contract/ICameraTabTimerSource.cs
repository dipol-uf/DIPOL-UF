#nullable enable
using System;

namespace DIPOL_UF.UiComponents.Contract
{
    internal interface ICameraTabTimerSource
    {
        IObservable<TimeSpan?> JobRemainingTime();
    }
}