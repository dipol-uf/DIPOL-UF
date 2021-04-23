#nullable enable
using System;
using DIPOL_UF.Annotations;
using Microsoft.Toolkit.HighPerformance;

namespace DIPOL_UF
{
    internal static class MathHelper
    {
        public static double FWHMConst { get; } = 2 * Math.Sqrt(2 * Math.Log(2));

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
        public static (float Min, float Max) MinMax(this float[]? array) =>
            array is null ? default : MinMax((ReadOnlySpan<float>) array);
        public static (float Min, float Max) MinMax(this ReadOnlySpan<float> @this)
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

            return (float.NaN, float.NaN);
        }
        
        public static (float Min, float Max) MinMax(this ReadOnlySpan2D<float> @this)
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

            return (float.NaN, float.NaN);
        }

        public static void GridExpand<Tx, Ty, T>(
            ReadOnlySpan<Tx> x, ReadOnlySpan<Ty> y, Span2D<T> target, Func<Tx, Ty, T> function
        ) where Tx : unmanaged
            where Ty : unmanaged
            where T : unmanaged
        {
            if (target.Width != x.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (target.Height != y.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(y));
            }

            for (var i = 0; i < y.Length; i++)
            {
                for (var j = 0; j < x.Length; j++)
                {
                    target[i, j] = function(x[j], y[i]);
                }
            }
        }

        public static float[] GenerateLinearRangeF(int start, int end)
        {
            var n = end - start;
            if (n > 0)
            {
                var result = new float[n];
                for (var i = 0; i < n; i++)
                {
                    result[i] = i + start;
                }

                return result;
            }

            return Array.Empty<float>();
        }

        public static double DistanceEuclidean(ReadOnlySpan<double> x, ReadOnlySpan<float> y)
        {
            if (x.Length == y.Length)
            {
                if (x.Length != 0)
                {
                    var sum = 0.0;
                    for (var i = 0; i < x.Length; i++)
                    {
                        var temp = x[i] - y[i];
                        sum += temp * temp;
                    }

                    return Math.Sqrt(sum);
                }

                return 0;
            }

            return double.NaN;
        }
    }
}