using System;
using System.Reactive.Linq;
using DIPOL_UF.Jobs;
using DIPOL_UF.UiComponents.Contract;
using DynamicData.Binding;

namespace DIPOL_UF.UiComponents.Implementation
{
    internal sealed class JobProgressSource : IJobProgressSource
    {
        private readonly JobManager _jobManager;

        public JobProgressSource(JobManager jobManager)
        {
            _jobManager = jobManager;
        }

        public IObservable<JobProgress> GetJobProgress() =>
            _jobManager.WhenPropertyChanged(x => x.IsInProcess).Select(x =>
                    x.Value
                        ? x.Sender
                            .WhenPropertyChanged(y => y.CumulativeProgress)
                            .Select(y =>
                                new JobProgress(y.Sender.CumulativeProgress,
                                    y.Sender.TotalAcquisitionActionCount + y.Sender.BiasActionCount +
                                    y.Sender.DarkActionCount
                                )
                            )
                        : Observable.Return(new JobProgress(0, 1))
                )
                .Switch();

    }
}