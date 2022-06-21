#nullable enable
using System;
using DIPOL_UF.Services.Contract;

namespace DIPOL_UF.Services.Implementation
{
    internal sealed class CycleTimerManager : ICycleTimerManager, ICycleTimerSource
    {
        private const int ImageReadoutDelayMs = 25;
        private const int MotorRotationDelayMs = 200;


        private ICycleTimer? _timerInstance;
        private DateTimeOffset _end;
        private DateTimeOffset _start;
        
        public void StartMeasuring(CycleTimingInfo cycleTimingInfo)
        {
           _timerInstance = new CycleTimer(this);
           AdjustTiming(cycleTimingInfo);
        }

        public void StopMeasuring()
        {
            _timerInstance = null;
        }

        public void PauseMeasuring()
        {
            if (_timerInstance is not null)
            {
                _timerInstance = new ConstantCycleTimer(_timerInstance.GetRemainingTime());
            }
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
        }

        public ICycleTimer? GetIfRunning()
        {
            return _timerInstance;
        }

        private sealed class CycleTimer : ICycleTimer
        {
            private readonly CycleTimerManager _manager;

            public CycleTimer(CycleTimerManager manager)
            {
                _manager = manager;
            }
            
            public TimeSpan GetRemainingTime()
            {
                var remainingTime =  _manager._end - DateTimeOffset.UtcNow;

                return remainingTime.Ticks < 0 ? TimeSpan.Zero : remainingTime;
            }
        }
    }
}
