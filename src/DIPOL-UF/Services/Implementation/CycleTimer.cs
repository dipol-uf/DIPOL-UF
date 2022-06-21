using System;
using DIPOL_UF.Services.Contract;

namespace DIPOL_UF.Services.Implementation
{

    
    internal sealed class ConstantCycleTimer : ICycleTimer
    {
        private readonly TimeSpan _remainingTime;
        public ConstantCycleTimer(TimeSpan remainingTime)
        {
            _remainingTime = remainingTime;
        }
        public TimeSpan GetRemainingTime() => _remainingTime;
    }
}