#nullable enable
using System;
using System.Buffers;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;
using Microsoft.Toolkit.HighPerformance;

namespace System.Runtime.CompilerServices
{
    public class IsExternalInit : Attribute
    {
    }
}

namespace DIPOL_UF
{
    internal class FWHM2D
    {
        private const int StackAllocLimit = 72;
        public class FitParams
        {
            public double Scale { get; init; }
            public double Sigma1 { get; init; }
            public double Sigma2 { get; init; }
            public double X0 { get; init; }
            public double Y0 { get; init; }
            public double ZeroPoint { get; init; }
            public double AngleRad { get; init; }
            public double AngleDeg => AngleRad / Math.PI * 180;

            public Vector<double> ToVector()
            {
                Vector<double> v = Vector<double>.Build.Dense(7);
                v[0] = Scale;
                v[1] = Sigma1;
                v[2] = Sigma2;
                v[3] = X0;
                v[4] = Y0;
                v[5] = ZeroPoint;
                v[6] = AngleRad;
                return v;
            }

            public static FitParams FromVector(Vector<double> data) =>
                data.Count != 7
                    ? throw new ArgumentException()
                    : new FitParams
                    {
                        Scale = data[0],
                        Sigma1 = data[1],
                        Sigma2 = data[2],
                        X0 = data[3],
                        Y0 = data[4],
                        ZeroPoint = data[5],
                        AngleRad = data[6]
                    };
        }

        public static FitParams ComputeFullWidthHalfMax2D(ReadOnlySpan2D<double> data)
        {
            if (data.IsEmpty)
            {
                return new FitParams();
            }

            try
            {
                var (width, height) = (data.Width, data.Height);

                double[] x = Generate.LinearRange(0, width - 1);
                double[] y = Generate.LinearRange(0, height - 1);
                double[] dataAlloc = new double[data.Length];
                if (!data.TryCopyTo_Temp(dataAlloc))
                {
                    return new FitParams();
                }

                var (min, max) = data.MinMax();
                var fun = ObjectiveFunction.Value(v => Dist2D(FitParams.FromVector(v), x, y, dataAlloc));
                var fitResult = NelderMeadSimplex.Minimum(
                    fun,
                    new FitParams
                    {
                        Scale = max,
                        Sigma1 = x.Length / 4.0,
                        Sigma2 = y.Length / 4.0,
                        X0 = x.Length / 2.0,
                        Y0 = y.Length / 2.0,
                        ZeroPoint = min,
                        AngleRad = 0
                    }.ToVector(),
                    maximumIterations: 10_000
                );
                return FitParams.FromVector(fitResult.MinimizingPoint);
            }
            catch (Exception e)
            {
                if (Injector.GetLogger() is { } logger)
                {
                    logger.Error(e, "Failed fitting using {Method}.", nameof(FWHM2D));
                }
                
                return new FitParams();
            }
        }

        private static double ExpFunc2D(FitParams p, double x, double y)
        {
            var (cos, sin) = (Math.Cos(p.AngleRad), Math.Sin(p.AngleRad));
            var (x0, y0) = (
                p.X0 * cos - p.Y0 * sin,
                p.X0 * sin + p.Y0 * cos
            );
            return
                p.ZeroPoint
                + p.Scale // / (2 * Math.PI * p.Sigma1 * p.Sigma2)
                * Math.Exp(
                    -(x - x0) * (x - x0) / (2 * p.Sigma1 * p.Sigma1)
                    -(y - y0) * (y - y0) / (2 * p.Sigma2 * p.Sigma2)
                );
        }

        private static double Dist2D(FitParams p, ReadOnlySpan<double> x, ReadOnlySpan<double> y, double[] data, double[]? buffer = null)
        {
            if (data.Length != x.Length * y.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            if (buffer is null)
            {
                buffer = new double[data.Length];
            }

            GridExpand(x, y, new Span2D<double>(buffer, y.Length, x.Length), (lx, ly) => ExpFunc2D(p, lx, ly));

            return Distance.Euclidean(data, buffer);
        }

        private static void GridExpand(
            ReadOnlySpan<double> x, ReadOnlySpan<double> y, Span2D<double> target, Func<double, double, double> function
        )
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
        
    }
}