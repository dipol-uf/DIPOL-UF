#nullable enable
using System;
using System.Reactive.Linq;

namespace DIPOL_UF.UiComponents.Implementation
{
    internal sealed class AcquisitionProgressTimerSource : Contract.IAcquisitionProgressTimerSource
    {
        private readonly Services.Contract.IAcquisitionTimerSource _timerSource;

        public AcquisitionProgressTimerSource(Services.Contract.IAcquisitionTimerSource timerSource)
        {
            _timerSource = timerSource;
        }

        public IObservable<TimeSpan?> AcquisitionRemainingTime(IObservable<bool> isAcquiring) =>
            isAcquiring
                .Select(x =>
                    x
                        ? Observable.Interval(TimeSpan.FromMilliseconds(100)).Select(_ => _timerSource)
                        : Observable.Return<Services.Contract.IAcquisitionTimerSource?>(null)
                )
                .Switch()
                .Select(x => x?.GetIfRunning()?.GetRemainingTime());
    }
}