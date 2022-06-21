using System;
using DIPOL_UF.Services.Contract;

namespace DIPOL_UF.Services.Implementation
{
    internal sealed class CycleTimer : ICycleTimer
    {
        private DateTimeOffset _end = DateTimeOffset.UtcNow;
        public void UpdateTimes(DateTimeOffset end) => _end = end;

        public TimeSpan GetRemainingTime()
        {
            var remainingTime =  _end - DateTimeOffset.UtcNow;

            return remainingTime.Ticks < 0 ? TimeSpan.Zero : remainingTime;
        }
    }
}