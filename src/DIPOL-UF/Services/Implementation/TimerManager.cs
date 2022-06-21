#nullable enable
using System;
using DIPOL_UF.Services.Contract;

namespace DIPOL_UF.Services.Implementation
{
    internal abstract class TimerManager<T> : ITimerManager<T>, ITimerSource
    {
        private readonly ITimeOffsetCalculator<T> _offsetCalculator;
        private ITimer? _timerInstance;
        private DateTimeOffset _end;
        private DateTimeOffset _start;

        protected TimerManager(ITimeOffsetCalculator<T> offsetCalculator)
        {
            _offsetCalculator = offsetCalculator;
        }

        public void StartMeasuring(T cycleTimingInfo)
        {
           _timerInstance = new Timer(this);
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
                _timerInstance = new ConstantTimer(_timerInstance.GetRemainingTime());
            }
        }
        
        public void AdjustTiming(T cycleTimingInfo)
        {
            _start = DateTimeOffset.UtcNow;
           _end = _start + _offsetCalculator.CalculateOffset(cycleTimingInfo);
        }

        public ITimer? GetIfRunning() => _timerInstance;

        private sealed class Timer : ITimer
        {
            private readonly TimerManager<T> _manager;

            public Timer(TimerManager<T> manager)
            {
                _manager = manager;
            }
            
            public TimeSpan GetRemainingTime()
            {
                var remainingTime =  _manager._end - DateTimeOffset.UtcNow;

                return remainingTime.Ticks < 0 ? TimeSpan.Zero : remainingTime;
            }
        }

        private sealed class ConstantTimer : ITimer
        {
            private readonly TimeSpan _remainingTime;
            public ConstantTimer(TimeSpan remainingTime)
            {
                _remainingTime = remainingTime;
            }
            public TimeSpan GetRemainingTime() => _remainingTime;
        }
    }
}
