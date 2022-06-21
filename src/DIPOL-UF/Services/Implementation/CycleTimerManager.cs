#nullable enable
using System;
using DIPOL_UF.Services.Contract;

namespace DIPOL_UF.Services.Implementation
{
    internal sealed class CycleTimerManager : ICycleTimerManager, ICycleTimerSource
    {
        private readonly ICycleTimer? _timerInstance;

        public void StartMeasuring()
        {
        }

        public void StopMeasuring()
        {
        }

        public void AdjustTiming()
        {
        }

        public ICycleTimer? GetIfRunning()
        {
            return _timerInstance;
        }
    }
}
