#nullable enable
using System;
using System.Timers;
using DIPOL_UF.Services.Contract;
using Microsoft.Extensions.Logging;

namespace DIPOL_UF.Services.Implementation
{
    internal sealed class CycleTimerManager : ICycleTimerManager, ICycleTimerSource
    {
        private const int ImageReadoutDelayMs = 25;
        private const int MotorRotationDelayMs = 200;

        private readonly ILogger<CycleTimerManager> _logger;

        private CycleTimer? _timerInstance;
        private DateTimeOffset _end;
        private DateTimeOffset _start;
        
        public CycleTimerManager(ILogger<CycleTimerManager> logger)
        {
            _logger = logger;
        }

        public void StartMeasuring(CycleTimingInfo cycleTimingInfo)
        {
           _timerInstance = new CycleTimer();
           AdjustTiming(cycleTimingInfo);
        }

        private void Log(object sender, EventArgs e)
        {
            _logger.LogWarning("{Remaining}", _timerInstance!.GetRemainingTime());
        }
        
        public void StopMeasuring()
        {
            _timerInstance = null;
        }

        public void AdjustTiming(CycleTimingInfo cycleTimingInfo)
        {
            _start = DateTimeOffset.UtcNow;
            var offsetMs =
                (cycleTimingInfo.ExposureTime.TotalMilliseconds + ImageReadoutDelayMs) *
                cycleTimingInfo.ExposedCamActionsCount * cycleTimingInfo.CycleCount;

            offsetMs += MotorRotationDelayMs * cycleTimingInfo.MotorActionsCount;

            offsetMs += cycleTimingInfo.BiasCamActionsCount * ImageReadoutDelayMs;

            offsetMs += cycleTimingInfo.DarkCamActionsCount *
                        (cycleTimingInfo.ExposureTime.TotalMilliseconds + ImageReadoutDelayMs);
            
            _end = _start + TimeSpan.FromMilliseconds(offsetMs);
            _timerInstance?.UpdateTimes(_end);
        }

        public ICycleTimer? GetIfRunning()
        {
            return _timerInstance;
        }
    }
}
