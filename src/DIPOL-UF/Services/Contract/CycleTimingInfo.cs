﻿#nullable enable
using System;

namespace DIPOL_UF.Services.Contract
{
    internal sealed record CycleTimingInfo
    (
        TimeSpan ExposureTime, int CycleCount, int ExposedCamActionsCount, int MotorActionsCount = 0,
        int BiasCamActionsCount = 0, int DarkCamActionsCount = 0
    );
}