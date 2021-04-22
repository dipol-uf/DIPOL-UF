#nullable enable
using System;
using DIPOL_UF.Annotations;
using Microsoft.Toolkit.HighPerformance;

namespace DIPOL_UF
{
    internal static class MathHelper
    {

        public static (double Min, double Max) MinMax(this double[]? array) =>
            array is null ? default : MinMax((ReadOnlySpan<double>) array);
        public static (double Min, double Max) MinMax(this ReadOnlySpan<double> @this)
        {
            if (!@this.IsEmpty)
            {
                var result = (Min: @this[0], Max: @this[0]);
                for (var i = 0; i < @this.Length; i++)
                {
                    var temp = @this[i];
                    if (temp > result.Max)
                    {
                        result.Max = temp;
                    } 
                    else if (temp < result.Min)
                    {
                        result.Min = temp;
                    }
                }

                return result;
            }

            return (double.NaN, double.NaN);
        }
        public static (double Min, double Max) MinMax(this ReadOnlySpan2D<double> @this)
        {
            if (!@this.IsEmpty)
            {
                var result = (Min: @this[0, 0], Max: @this[0, 0]);
                for (var i = 0; i < @this.Height; i++)
                {
                    for (var j = 0; j < @this.Width; j++)
                    {
                        var temp = @this[i, j];
                        if (temp > result.Max)
                        {
                            result.Max = temp;
                        } 
                        else if (temp < result.Min)
                        {
                            result.Min = temp;
                        }
                    }
                }
                
                return result;
            }

            return (double.NaN, double.NaN);
        }
    }
}