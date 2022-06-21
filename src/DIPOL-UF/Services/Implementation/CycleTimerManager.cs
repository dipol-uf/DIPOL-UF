#nullable enable
using DIPOL_UF.Services.Contract;

namespace DIPOL_UF.Services.Implementation
{
    internal sealed class CycleTimerManager : TimerManager<CycleTimingInfo>, ICycleTimerManager, ICycleTimerSource
    {
        public CycleTimerManager(ITimeOffsetCalculator<CycleTimingInfo> offsetCalculator) : base(offsetCalculator)
        {
        }
    }
}