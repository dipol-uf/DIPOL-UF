#nullable enable
using System;
using DIPOL_UF.Services.Contract;

namespace DIPOL_UF.Services.Implementation
{
    internal abstract class TimerManager<T> : ITimerManager<T>, ITimerSource
    {
        private ITimer? _timerInstance;
        private DateTimeOffset _end;
        private DateTimeOffset _start;

        public void StartMeasuring(T timingInfo)
        {
            _timerInstance = new Timer(this);
            AdjustTiming(timingInfo);
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

        public void AdjustTiming(T timingInfo)
        {
            _start = DateTimeOffset.UtcNow;
            _end = _start + CalculateOffset(timingInfo);
        }

        public ITimer? GetIfRunning() => _timerInstance;

        protected abstract TimeSpan CalculateOffset(T value);

        private sealed class Timer : ITimer
        {
            private readonly TimerManager<T> _manager;

            public Timer(TimerManager<T> manager)
            {
                _manager = manager;
            }

            public TimeSpan GetRemainingTime()
            {
                var remainingTime = _manager._end - DateTimeOffset.UtcNow;

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

    internal sealed class CycleTimerManager : TimerManager<CycleTimingInfo>, ICycleTimerManager, ICycleTimerSource
    {
        private const int ImageReadoutDelayMs = 150;
        private const int MotorRotationDelayMs = 700;

        protected override TimeSpan CalculateOffset(CycleTimingInfo value)
        {
            var offsetMs =
                (value.ExposureTime.TotalMilliseconds + ImageReadoutDelayMs) *
                value.ExposedCamActionsCount * value.CycleCount;

            offsetMs += MotorRotationDelayMs * value.MotorActionsCount * value.CycleCount;

            offsetMs += value.BiasCamActionsCount * ImageReadoutDelayMs;

            offsetMs += value.DarkCamActionsCount *
                        (value.ExposureTime.TotalMilliseconds + ImageReadoutDelayMs);

            return TimeSpan.FromMilliseconds(offsetMs);
        }

    }

    internal sealed class AcquisitionTimerManger : TimerManager<TimeSpan>,
                                                   IAcquisitionTimerManager,
                                                   IAcquisitionTimerSource
    {
        protected override TimeSpan CalculateOffset(TimeSpan value) => value;
    }
}
