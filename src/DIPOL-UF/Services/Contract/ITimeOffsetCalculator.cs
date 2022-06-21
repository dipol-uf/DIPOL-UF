using System;

namespace DIPOL_UF.Services.Contract
{
    internal interface ITimeOffsetCalculator<T>
    {
        TimeSpan CalculateOffset(T value);
    }
}