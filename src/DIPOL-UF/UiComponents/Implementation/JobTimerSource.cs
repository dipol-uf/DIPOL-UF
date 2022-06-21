#nullable enable
using System;
using System.Reactive.Linq;
using DIPOL_UF.Jobs;
using DIPOL_UF.Services.Contract;
using DIPOL_UF.UiComponents.Contract;
using DynamicData.Binding;

namespace DIPOL_UF.UiComponents.Implementation
{
    internal sealed class JobTimerSource : IJobTimerSource
    {
        private readonly ICycleTimerSource _timerSource;
        private readonly JobManager _jobManager;

        public JobTimerSource(ICycleTimerSource timerSource, JobManager jobManager)
        {
            _timerSource = timerSource;
            _jobManager = jobManager;
        }

        public IObservable<TimeSpan?> JobRemainingTime() =>
            _jobManager.WhenPropertyChanged(x => x.IsInProcess)
                .Select(inProgress =>
                    inProgress.Value
                        ? Observable.Interval(TimeSpan.FromMilliseconds(100)).Select(_ => _timerSource)
                        : Observable.Return<ICycleTimerSource?>(null)
                )
                .Switch()
                .Select(x => x?.GetIfRunning()?.GetRemainingTime());

    }
}