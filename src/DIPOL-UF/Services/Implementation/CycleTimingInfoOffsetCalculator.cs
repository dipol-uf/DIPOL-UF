#nullable enable
using System;
using DIPOL_UF.Services.Contract;

namespace DIPOL_UF.Services.Implementation
{
    internal sealed class CycleTimingInfoOffsetCalculator : ITimeOffsetCalculator<CycleTimingInfo>
    {
        private const int ImageReadoutDelayMs = 25;
        private const int MotorRotationDelayMs = 200;

        public TimeSpan CalculateOffset(CycleTimingInfo value)
        {
            var offsetMs =
                (value.ExposureTime.TotalMilliseconds + ImageReadoutDelayMs) *
                value.ExposedCamActionsCount * value.CycleCount;
            
            offsetMs += MotorRotationDelayMs * value.MotorActionsCount;
            
            offsetMs += value.BiasCamActionsCount * ImageReadoutDelayMs;
            
            offsetMs += value.DarkCamActionsCount *
                        (value.ExposureTime.TotalMilliseconds + ImageReadoutDelayMs);
            
            return  TimeSpan.FromMilliseconds(offsetMs);
        }
            
    }
}