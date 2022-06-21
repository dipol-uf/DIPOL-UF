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
        private readonly Timer _timer = new();

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

            _timer.Interval = 250;
            _timer.AutoReset = true;
            _timer.Elapsed += Log;
            _timer.Start();
        }

        private void Log(object sender, EventArgs e)
        {
            _logger.LogWarning("{Remaining}", _timerInstance!.GetRemainingTime());
        }
        
        public void StopMeasuring()
        {
            _timerInstance = null;
            _timer.Stop();
            _timer.Elapsed -= Log;
        }

        public void AdjustTiming(CycleTimingInfo cycleTimingInfo)
        {
            _start = DateTimeOffset.UtcNow;
            var offsetMs =
                (cycleTimingInfo.ExposureTime.TotalMilliseconds + ImageReadoutDelayMs + MotorRotationDelayMs) *
                cycleTimingInfo.ExposedCamActionsCount * cycleTimingInfo.CycleCount;

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
