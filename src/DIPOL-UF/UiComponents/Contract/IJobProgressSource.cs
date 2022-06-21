using System;

namespace DIPOL_UF.UiComponents.Contract
{
    internal sealed record JobProgress(int Done, int Total);
    internal interface IJobProgressSource
    {
        IObservable<JobProgress> GetJobProgress();
    }
}